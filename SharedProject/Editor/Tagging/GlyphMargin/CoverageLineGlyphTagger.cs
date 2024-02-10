using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using System.Collections.Generic;
using System;

namespace FineCodeCoverage.Impl
{
    internal class CoverageLineGlyphTagger : ITagger<CoverageLineGlyphTag>, IDisposable, IListener<CoverageColoursChangedMessage>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageTagger<CoverageLineGlyphTag> coverageTagger;

        public CoverageLineGlyphTagger(IEventAggregator eventAggregator, ICoverageTagger<CoverageLineGlyphTag> coverageTagger)
        {
            ThrowIf.Null(coverageTagger, nameof(coverageTagger));
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.coverageTagger = coverageTagger;

        }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { coverageTagger.TagsChanged += value; }
            remove { coverageTagger.TagsChanged -= value; }
        }

        public void Dispose()
        {
            coverageTagger.Dispose();
            eventAggregator.RemoveListener(this);
        }

        public IEnumerable<ITagSpan<CoverageLineGlyphTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return coverageTagger.GetTags(spans);
        }

        public void Handle(CoverageColoursChangedMessage message)
        {
            if (coverageTagger.HasCoverage)
            {
                coverageTagger.RaiseTagsChanged();
            }
        }
    }
}