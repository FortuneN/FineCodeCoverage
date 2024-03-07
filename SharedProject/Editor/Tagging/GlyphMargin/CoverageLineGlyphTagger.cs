using System;
using System.Collections.Generic;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    internal class CoverageLineGlyphTagger : ITagger<CoverageLineGlyphTag>, IDisposable, IListener<CoverageColoursChangedMessage>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageTagger<CoverageLineGlyphTag> coverageTagger;

        public CoverageLineGlyphTagger(IEventAggregator eventAggregator, ICoverageTagger<CoverageLineGlyphTag> coverageTagger)
        {
            ThrowIf.Null(coverageTagger, nameof(coverageTagger));
            _ = eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.coverageTagger = coverageTagger;

        }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add => this.coverageTagger.TagsChanged += value;
            remove => this.coverageTagger.TagsChanged -= value;
        }

        public void Dispose()
        {
            this.coverageTagger.Dispose();
            _ = this.eventAggregator.RemoveListener(this);
        }

        public IEnumerable<ITagSpan<CoverageLineGlyphTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            => this.coverageTagger.GetTags(spans);

        public void Handle(CoverageColoursChangedMessage message)
        {
            if (this.coverageTagger.HasCoverage)
            {
                this.coverageTagger.RaiseTagsChanged();
            }
        }
    }
}