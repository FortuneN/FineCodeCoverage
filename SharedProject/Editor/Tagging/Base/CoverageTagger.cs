using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    internal class CoverageTagger<TTag> :
        ICoverageTagger<TTag>,
        IListener<CoverageTypeFilterChangedMessage>,
        IListener<CoverageChangedMessage>,
        IDisposable,
        ITagger<TTag>
        where TTag : ITag

    {
        private readonly ITextBuffer textBuffer;
        private readonly string filePath;
        private IBufferLineCoverage coverageLines;
        private ICoverageTypeFilter coverageTypeFilter;
        private readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly ILineSpanTagger<TTag> lineSpanTagger;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CoverageTagger(
            ITextBufferWithFilePath textBufferWithFilePath,
            IBufferLineCoverage lastCoverageLines,
            ICoverageTypeFilter coverageTypeFilter,
            IEventAggregator eventAggregator,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> lineSpanTagger
        )
        {
            ThrowIf.Null(textBufferWithFilePath, nameof(textBufferWithFilePath));
            ThrowIf.Null(coverageTypeFilter, nameof(coverageTypeFilter));
            ThrowIf.Null(eventAggregator, nameof(eventAggregator));
            ThrowIf.Null(lineSpanLogic, nameof(lineSpanLogic));
            ThrowIf.Null(lineSpanTagger, nameof(lineSpanTagger));
            this.textBuffer = textBufferWithFilePath.TextBuffer;
            this.filePath = textBufferWithFilePath.FilePath;
            this.coverageLines = lastCoverageLines;
            this.coverageTypeFilter = coverageTypeFilter;
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
            this.lineSpanTagger = lineSpanTagger;
            eventAggregator.AddListener(this);
        }

        public bool HasCoverage => coverageLines != null;

        public void RaiseTagsChanged()
        {
            var span = new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length);
            var spanEventArgs = new SnapshotSpanEventArgs(span);
            TagsChanged?.Invoke(this, spanEventArgs);
        }

        
        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (coverageLines == null || coverageTypeFilter.Disabled)
            {
                return Enumerable.Empty<ITagSpan<TTag>>();
            }
            var lineSpans = lineSpanLogic.GetLineSpans(coverageLines, spans);
            return GetTags(lineSpans);
        }

        private IEnumerable<ITagSpan<TTag>> GetTags(IEnumerable<ILineSpan> lineSpans)
        {
            foreach (var lineSpan in lineSpans)
            {
                if (!coverageTypeFilter.Show(lineSpan.Line.CoverageType))
                {
                    continue;
                }
                yield return lineSpanTagger.GetTagSpan(lineSpan);
            }
        }

        public void Dispose()
        {
            eventAggregator.RemoveListener(this);
        }

        public void Handle(CoverageChangedMessage message)
        {
            coverageLines = message.CoverageLines;
            if(message.AppliesTo == filePath)
            {
                RaiseTagsChanged();
            }
        }

        public void Handle(CoverageTypeFilterChangedMessage message)
        {
            if (message.Filter.TypeIdentifier == coverageTypeFilter.TypeIdentifier)
            {
                coverageTypeFilter = message.Filter;
                if (HasCoverage)
                {
                    RaiseTagsChanged();
                }
            }
        }
    }

}
