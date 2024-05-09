using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class BufferLineCoverage : 
        IBufferLineCoverage, IListener<NewCoverageLinesMessage>, IListener<TestExecutionStartingMessage>
    {
        private readonly ITextInfo textInfo;
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;
        private readonly ITextBuffer2 textBuffer;
        private ITrackedLines trackedLines;
        private bool? editorCoverageModeOff;
        private IFileLineCoverage fileLineCoverage;
        private Nullable<DateTime> lastChanged;
        private DateTime lastTestExecutionStarting; 
        public BufferLineCoverage(
            IFileLineCoverage fileLineCoverage,
            ITextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory,
            IDynamicCoverageStore dynamicCoverageStore,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger
        )
        {
            this.fileLineCoverage = fileLineCoverage;
            this.textBuffer = textInfo.TextBuffer;
            this.textInfo = textInfo;
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
            this.dynamicCoverageStore = dynamicCoverageStore;
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            void AppOptionsChanged(IAppOptions appOptions)
            {
                bool newEditorCoverageModeOff = appOptions.EditorCoverageColouringMode == EditorCoverageColouringMode.Off;
                if (this.trackedLines != null && newEditorCoverageModeOff && this.editorCoverageModeOff != newEditorCoverageModeOff)
                {
                    this.trackedLines = null;
                    this.SendCoverageChangedMessage();
                }

                this.editorCoverageModeOff = newEditorCoverageModeOff;
            }

            appOptionsProvider.OptionsChanged += AppOptionsChanged;
            if (fileLineCoverage != null)
            {
                this.CreateTrackedLinesIfRequired(true);
            }

            _ = eventAggregator.AddListener(this);
            this.textBuffer.ChangedOnBackground += this.TextBuffer_ChangedOnBackground;
            void textViewClosedHandler(object s, EventArgs e)
            {
                this.UpdateDynamicCoverageStore((s as ITextView).TextSnapshot.GetText());
                this.textBuffer.Changed -= this.TextBuffer_ChangedOnBackground;
                textInfo.TextView.Closed -= textViewClosedHandler;
                appOptionsProvider.OptionsChanged -= AppOptionsChanged;
                _ = eventAggregator.RemoveListener(this);
            }

            textInfo.TextView.Closed += textViewClosedHandler;
        }

        private void UpdateDynamicCoverageStore(string text)
        {
            if (this.trackedLines != null)
            {
                this.dynamicCoverageStore.SaveSerializedCoverage(
                    this.textInfo.FilePath, 
                    this.trackedLinesFactory.Serialize(this.trackedLines, text)
                );
            }
            else
            {
                this.dynamicCoverageStore.RemoveSerializedCoverage(this.textInfo.FilePath);
            }
        }

        private void CreateTrackedLinesIfRequired(bool initial)
        {
            if (this.EditorCoverageColouringModeOff())
            {
                this.trackedLines = null;
            }
            else
            {
                this.TryCreateTrackedLines(initial);
            }
        }

        private void TryCreateTrackedLines(bool initial)
        {
            try
            {
                this.CreateTrackedLines(initial);
            }
            catch (Exception e)
            {
                this.logger.Log($"Error creating tracked lines for {this.textInfo.FilePath}", e);
            }
        }

        private void CreateTrackedLinesIfRequiredWithMessage()
        {
            bool hadTrackedLines = this.trackedLines != null;
            if (!this.lastChanged.HasValue || this.lastChanged < this.lastTestExecutionStarting)
            {
                this.CreateTrackedLinesIfRequired(false);
            }
            else
            {
                this.logger.Log($"Not creating editor marks for {this.textInfo.FilePath} as it was changed after test execution started");
                this.trackedLines = null;
            }

            bool hasTrackedLines = this.trackedLines != null;
            if (hadTrackedLines || hasTrackedLines)
            {
                this.SendCoverageChangedMessage();
            }
        }

        private void CreateTrackedLines(bool initial)
        {
            string filePath = this.textInfo.FilePath;
            ITextSnapshot currentSnapshot = this.textBuffer.CurrentSnapshot;
            if (initial)
            {
                string serializedCoverage = this.dynamicCoverageStore.GetSerializedCoverage(filePath);
                if (serializedCoverage != null)
                {
                    this.trackedLines = this.trackedLinesFactory.Create(serializedCoverage, currentSnapshot, filePath);
                    return;
                }
            }

            var lines = this.fileLineCoverage.GetLines(this.textInfo.FilePath).ToList();
            this.trackedLines = this.trackedLinesFactory.Create(lines, currentSnapshot, filePath);
        }

        private bool EditorCoverageColouringModeOff()
        {
            this.editorCoverageModeOff = this.appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.Off;
            return this.editorCoverageModeOff.Value;
        }

        private void TextBuffer_ChangedOnBackground(object sender, TextContentChangedEventArgs textContentChangedEventArgs)
        {
            this.lastChanged = DateTime.Now;
            if (this.trackedLines != null)
            {
                this.TryUpdateTrackedLines(textContentChangedEventArgs);
            }
        }

        private void TryUpdateTrackedLines(TextContentChangedEventArgs textContentChangedEventArgs)
        {
            try
            {
                this.UpdateTrackedLines(textContentChangedEventArgs);
            }
            catch (Exception e)
            {
                this.logger.Log($"Error updating tracked lines for {this.textInfo.FilePath}", e);
            }
        }

        private void UpdateTrackedLines(TextContentChangedEventArgs textContentChangedEventArgs)
        {
            IEnumerable<int> changedLineNumbers = this.trackedLines.GetChangedLineNumbers(textContentChangedEventArgs.After, textContentChangedEventArgs.Changes.Select(change => change.NewSpan).ToList())
                .Where(changedLine => changedLine >= 0 && changedLine < textContentChangedEventArgs.After.LineCount);
            this.SendCoverageChangedMessageIfChanged(changedLineNumbers);
        }

        private void SendCoverageChangedMessageIfChanged(IEnumerable<int> changedLineNumbers)
        {
            if (changedLineNumbers.Any())
            {
                this.SendCoverageChangedMessage(changedLineNumbers);
            }
        }

        private void SendCoverageChangedMessage(IEnumerable<int> changedLineNumbers = null)
            => this.eventAggregator.SendMessage(new CoverageChangedMessage(this, this.textInfo.FilePath, changedLineNumbers));

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
            => this.trackedLines == null ? Enumerable.Empty<IDynamicLine>() : this.trackedLines.GetLines(startLineNumber, endLineNumber);

        public void Handle(NewCoverageLinesMessage message)
        {
            this.fileLineCoverage = message.CoverageLines;

            bool hadTrackedLines = this.trackedLines != null;
            if (this.fileLineCoverage == null)
            {
                this.trackedLines = null;
                if (hadTrackedLines)
                {
                    this.SendCoverageChangedMessage();
                }
            }
            else
            {
                this.CreateTrackedLinesIfRequiredWithMessage();
            }
        }

        public void Handle(TestExecutionStartingMessage message) => this.lastTestExecutionStarting = DateTime.Now;
    }
}
