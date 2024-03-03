using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class BufferLineCoverage : IBufferLineCoverage, IListener<NewCoverageLinesMessage>
    {
        private readonly ITextInfo textInfo;
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly Language language;
        private readonly ITextBuffer2 textBuffer;
        private ITrackedLines trackedLines;
        private bool? editorCoverageModeOff;
        private IFileLineCoverage fileLineCoverage;
        public BufferLineCoverage(
            IFileLineCoverage fileLineCoverage,
            ITextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory,
            IDynamicCoverageStore dynamicCoverageStore,
            IAppOptionsProvider appOptionsProvider
        )
        {
            this.fileLineCoverage = fileLineCoverage;
            this.language = SupportedContentTypeLanguages.GetLanguage(textInfo.TextBuffer.ContentType.TypeName);
            this.textBuffer = textInfo.TextBuffer;
            this.textInfo = textInfo;
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
            this.dynamicCoverageStore = dynamicCoverageStore;
            this.appOptionsProvider = appOptionsProvider;
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
                this.UpdateDynamicCoverageStore();
                this.textBuffer.Changed -= this.TextBuffer_ChangedOnBackground;
                textInfo.TextView.Closed -= textViewClosedHandler;
                appOptionsProvider.OptionsChanged -= AppOptionsChanged;
                _ = eventAggregator.RemoveListener(this);
            }

            textInfo.TextView.Closed += textViewClosedHandler;
        }

        private void UpdateDynamicCoverageStore()
        {
            if (this.trackedLines != null)
            {
                this.dynamicCoverageStore.SaveSerializedCoverage(this.textInfo.FilePath, this.trackedLinesFactory.Serialize(this.trackedLines));
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
                this.CreateTrackedLines(initial);
            }
        }

        private void CreateTrackedLinesIfRequiredWithMessage()
        {
            bool hadTrackedLines = this.trackedLines != null;
            this.CreateTrackedLinesIfRequired(false);
            bool hasTrackedLines = this.trackedLines != null;
            if (hadTrackedLines || hasTrackedLines)
            {
                this.SendCoverageChangedMessage();
            }
        }

        private void CreateTrackedLines(bool initial)
        {
            ITextSnapshot currentSnapshot = this.textBuffer.CurrentSnapshot;
            if (initial)
            {
                string serializedCoverage = this.dynamicCoverageStore.GetSerializedCoverage(this.textInfo.FilePath);
                if (serializedCoverage != null)
                {
                    this.trackedLines = this.trackedLinesFactory.Create(serializedCoverage, currentSnapshot, this.language);
                    return;
                }
            }

            var lines = this.fileLineCoverage.GetLines(this.textInfo.FilePath).ToList();
            this.trackedLines = this.trackedLinesFactory.Create(lines, currentSnapshot, this.language);
        }

        private bool EditorCoverageColouringModeOff()
        {
            this.editorCoverageModeOff = this.appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.Off;
            return this.editorCoverageModeOff.Value;
        }

        private void TextBuffer_ChangedOnBackground(object sender, TextContentChangedEventArgs e)
        {
            if (this.trackedLines != null)
            {
                bool changed = this.trackedLines.Changed(e.After, e.Changes.Select(change => change.NewSpan).ToList());
                if (changed)
                {
                    this.SendCoverageChangedMessage();
                }
            }
        }

        private void SendCoverageChangedMessage() => this.eventAggregator.SendMessage(new CoverageChangedMessage(this, this.textInfo.FilePath));

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
    }
}
