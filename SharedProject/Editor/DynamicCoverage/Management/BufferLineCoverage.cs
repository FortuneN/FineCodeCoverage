using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

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
            language = SupportedContentTypeLanguages.GetLanguage(textInfo.TextBuffer.ContentType.TypeName);
            this.textBuffer = textInfo.TextBuffer;
            this.textInfo = textInfo;
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
            this.dynamicCoverageStore = dynamicCoverageStore;
            this.appOptionsProvider = appOptionsProvider;
            void AppOptionsChanged(IAppOptions appOptions)
            {
                var newEditorCoverageModeOff = appOptions.EditorCoverageColouringMode == EditorCoverageColouringMode.Off;
                if (trackedLines != null && newEditorCoverageModeOff && editorCoverageModeOff != newEditorCoverageModeOff)
                {
                    trackedLines = null;
                    SendCoverageChangedMessage();
                }
                editorCoverageModeOff = newEditorCoverageModeOff;
            }
            appOptionsProvider.OptionsChanged += AppOptionsChanged;
            if (fileLineCoverage != null)
            {
                CreateTrackedLinesIfRequired(true);
            }
            eventAggregator.AddListener(this);
            textBuffer.ChangedOnBackground += TextBuffer_ChangedOnBackground;
            void textViewClosedHandler(object s, EventArgs e)
            {
                UpdateDynamicCoverageStore();
                textBuffer.Changed -= TextBuffer_ChangedOnBackground;
                textInfo.TextView.Closed -= textViewClosedHandler;
                appOptionsProvider.OptionsChanged -= AppOptionsChanged;
                eventAggregator.RemoveListener(this);
            }

            textInfo.TextView.Closed += textViewClosedHandler;
        }

        private void UpdateDynamicCoverageStore()
        {
            if (trackedLines != null)
            {
                dynamicCoverageStore.SaveSerializedCoverage(textInfo.FilePath, trackedLinesFactory.Serialize(trackedLines));
            }
            else
            {
                dynamicCoverageStore.RemoveSerializedCoverage(textInfo.FilePath);
            }
        }

        private void CreateTrackedLinesIfRequired(bool initial)
        {
            if (EditorCoverageColouringModeOff())
            {
                trackedLines = null;
            }
            else
            {
                CreateTrackedLines(initial);
            }
        }


        private void CreateTrackedLinesIfRequiredWithMessage()
        {
            var hadTrackedLines = trackedLines != null;
            CreateTrackedLinesIfRequired(false);
            var hasTrackedLines = trackedLines != null;
            if ((hadTrackedLines || hasTrackedLines))
            {
                SendCoverageChangedMessage();
            }
        }

        private void CreateTrackedLines(bool initial)
        {
            var currentSnapshot = textBuffer.CurrentSnapshot;
            if (initial)
            {
                var serializedCoverage = dynamicCoverageStore.GetSerializedCoverage(textInfo.FilePath);
                if (serializedCoverage != null)
                {
                    trackedLines = trackedLinesFactory.Create(serializedCoverage, currentSnapshot, language);
                    return;
                }
            }

            var lines = fileLineCoverage.GetLines(textInfo.FilePath).ToList();
            trackedLines = trackedLinesFactory.Create(lines, currentSnapshot, language);
        }

        private bool EditorCoverageColouringModeOff()
        {
            editorCoverageModeOff = appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.Off;
            return editorCoverageModeOff.Value;
        }

        private void TextBuffer_ChangedOnBackground(object sender, TextContentChangedEventArgs e)
        {
            if (trackedLines != null)
            {
                var changed = trackedLines.Changed(e.After, e.Changes.Select(change => change.NewSpan).ToList());
                if (changed)
                {
                    SendCoverageChangedMessage();
                }
            }
        }

        private void SendCoverageChangedMessage()
        {
            eventAggregator.SendMessage(new CoverageChangedMessage(this, textInfo.FilePath));
        }

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
            if (trackedLines == null)
            {
                return Enumerable.Empty<IDynamicLine>();
            }
            return trackedLines.GetLines(startLineNumber, endLineNumber);
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            fileLineCoverage = message.CoverageLines;

            var hadTrackedLines = trackedLines != null;
            if (fileLineCoverage == null)
            {
                trackedLines = null;
                if (hadTrackedLines)
                {
                    SendCoverageChangedMessage();
                }
            }
            else
            {
                CreateTrackedLinesIfRequiredWithMessage();
            }
        }
    }
}
