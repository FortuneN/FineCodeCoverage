using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.IndicatorVisibility;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal class CoverageTagger<TTag> :
        ICoverageTagger<TTag>,
        IListener<CoverageTypeFilterChangedMessage>,
        IListener<CoverageChangedMessage>,
        IDisposable,
        ITagger<TTag>
        where TTag : ITag

    {
        private readonly ITextInfo textInfo;
        private readonly ITextBuffer textBuffer;
        private IBufferLineCoverage bufferLineCoverage;
        private ICoverageTypeFilter coverageTypeFilter;
        private readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly ILineSpanTagger<TTag> lineSpanTagger;
        private readonly IFileIndicatorVisibility fileIndicatorVisibility;
        private bool isDisplayingIndicators;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CoverageTagger(
            ITextInfo textInfo,
            IBufferLineCoverage bufferLineCoverage,
            ICoverageTypeFilter coverageTypeFilter,
            IEventAggregator eventAggregator,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> lineSpanTagger,
            IFileIndicatorVisibility fileIndicatorVisibility)
        {
            ThrowIf.Null(textInfo, nameof(textInfo));
            ThrowIf.Null(coverageTypeFilter, nameof(coverageTypeFilter));
            ThrowIf.Null(eventAggregator, nameof(eventAggregator));
            ThrowIf.Null(lineSpanLogic, nameof(lineSpanLogic));
            ThrowIf.Null(lineSpanTagger, nameof(lineSpanTagger));
            this.textInfo = textInfo;
            this.textBuffer = textInfo.TextBuffer;
            this.bufferLineCoverage = bufferLineCoverage;
            this.coverageTypeFilter = coverageTypeFilter;
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
            this.lineSpanTagger = lineSpanTagger;
            this.fileIndicatorVisibility = fileIndicatorVisibility;
            this.isDisplayingIndicators = fileIndicatorVisibility.IsVisible(textInfo.FilePath);
            fileIndicatorVisibility.VisibilityChanged += this.FileIndicatorVisibility_VisibilityChanged;
            _ = eventAggregator.AddListener(this);
        }

        private void FileIndicatorVisibility_VisibilityChanged(object sender, EventArgs e)
        {
            bool newIsDisplayingIndicators = this.fileIndicatorVisibility.IsVisible(this.textInfo.FilePath);
            bool visibilityChanged = newIsDisplayingIndicators != this.isDisplayingIndicators;
            if (visibilityChanged)
            {
                this.isDisplayingIndicators = newIsDisplayingIndicators;
                this.RaiseTagsChanged();
            }
        }

        public bool HasCoverage => this.bufferLineCoverage != null;

        public void RaiseTagsChanged() => this.RaiseTagsChangedLinesOrAll();

        private void RaiseTagsChangedLinesOrAll(IEnumerable<int> changedLines = null)
        {
            ITextSnapshot currentSnapshot = this.textBuffer.CurrentSnapshot;
            SnapshotSpan snapshotSpan;
            if (changedLines != null)
            {
                Span span = changedLines.Select(changedLine => currentSnapshot.GetLineFromLineNumber(changedLine).Extent.Span)
                    .Aggregate((acc, next) => Span.FromBounds(Math.Min(acc.Start, next.Start), Math.Max(acc.End, next.End)));
                snapshotSpan = new SnapshotSpan(currentSnapshot, span);
            }
            else
            {
                snapshotSpan = new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length);
            }

            var spanEventArgs = new SnapshotSpanEventArgs(snapshotSpan);
            TagsChanged?.Invoke(this, spanEventArgs);
        }

        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            => this.CanGetTagsFromCoverageLines
                ? this.GetTagsFromCoverageLines(spans)
                : Enumerable.Empty<ITagSpan<TTag>>();

        private bool CanGetTagsFromCoverageLines => this.bufferLineCoverage != null && !this.coverageTypeFilter.Disabled && this.isDisplayingIndicators;

        private IEnumerable<ITagSpan<TTag>> GetTagsFromCoverageLines(NormalizedSnapshotSpanCollection spans)
        {
            IEnumerable<ILineSpan> lineSpans = this.lineSpanLogic.GetLineSpans(this.bufferLineCoverage, spans);
            return this.GetTags(lineSpans);
        }

        private IEnumerable<ITagSpan<TTag>> GetTags(IEnumerable<ILineSpan> lineSpans)
            => lineSpans.Where(lineSpan => this.coverageTypeFilter.Show(lineSpan.Line.CoverageType))
                .Select(lineSpan => this.lineSpanTagger.GetTagSpan(lineSpan));

        public void Dispose() {
            _ = this.eventAggregator.RemoveListener(this);
            this.fileIndicatorVisibility.VisibilityChanged -= this.FileIndicatorVisibility_VisibilityChanged;
        }

        public void Handle(CoverageChangedMessage message)
        {
            if (this.IsOwnChange(message))
            {
                this.HandleOwnChange(message);
            }
        }

        private bool IsOwnChange(CoverageChangedMessage message) => message.AppliesTo == this.textInfo.FilePath;

        private void HandleOwnChange(CoverageChangedMessage message)
        {
            this.bufferLineCoverage = message.BufferLineCoverage;
            this.RaiseTagsChangedLinesOrAll(message.ChangedLineNumbers);
        }

        public void Handle(CoverageTypeFilterChangedMessage message)
        {
            if (message.Filter.TypeIdentifier == this.coverageTypeFilter.TypeIdentifier)
            {
                this.coverageTypeFilter = message.Filter;
                if (this.HasCoverage)
                {
                    this.RaiseTagsChanged();
                }
            }
        }
    }
}
