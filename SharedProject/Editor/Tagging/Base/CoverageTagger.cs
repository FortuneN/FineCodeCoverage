using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
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
        private IBufferLineCoverage coverageLines;
        private ICoverageTypeFilter coverageTypeFilter;
        private readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly ILineSpanTagger<TTag> lineSpanTagger;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CoverageTagger(
            ITextInfo textInfo,
            IBufferLineCoverage lastCoverageLines,
            ICoverageTypeFilter coverageTypeFilter,
            IEventAggregator eventAggregator,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> lineSpanTagger
        )
        {
            ThrowIf.Null(textInfo, nameof(textInfo));
            ThrowIf.Null(coverageTypeFilter, nameof(coverageTypeFilter));
            ThrowIf.Null(eventAggregator, nameof(eventAggregator));
            ThrowIf.Null(lineSpanLogic, nameof(lineSpanLogic));
            ThrowIf.Null(lineSpanTagger, nameof(lineSpanTagger));
            this.textInfo = textInfo;
            this.textBuffer = textInfo.TextBuffer;
            this.coverageLines = lastCoverageLines;
            this.coverageTypeFilter = coverageTypeFilter;
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
            this.lineSpanTagger = lineSpanTagger;
            _ = eventAggregator.AddListener(this);
        }

        public bool HasCoverage => this.coverageLines != null;

        public void RaiseTagsChanged()
        {
            var span = new SnapshotSpan(this.textBuffer.CurrentSnapshot, 0, this.textBuffer.CurrentSnapshot.Length);
            var spanEventArgs = new SnapshotSpanEventArgs(span);
            TagsChanged?.Invoke(this, spanEventArgs);
        }

        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.coverageLines == null || this.coverageTypeFilter.Disabled)
            {
                return Enumerable.Empty<ITagSpan<TTag>>();
            }

            IEnumerable<ILineSpan> lineSpans = this.lineSpanLogic.GetLineSpans(this.coverageLines, spans);
            return this.GetTags(lineSpans);
        }

        private IEnumerable<ITagSpan<TTag>> GetTags(IEnumerable<ILineSpan> lineSpans)
        {
            foreach (ILineSpan lineSpan in lineSpans)
            {
                if (!this.coverageTypeFilter.Show(lineSpan.Line.CoverageType))
                {
                    continue;
                }

                yield return this.lineSpanTagger.GetTagSpan(lineSpan);
            }
        }

        public void Dispose() => _ = this.eventAggregator.RemoveListener(this);

        public void Handle(CoverageChangedMessage message)
        {
            this.coverageLines = message.CoverageLines;
            if (message.AppliesTo == this.textInfo.FilePath)
            {
                this.RaiseTagsChanged();
            }
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
