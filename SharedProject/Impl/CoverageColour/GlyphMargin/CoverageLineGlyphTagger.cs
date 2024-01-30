using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTagger : CoverageLineTaggerBase<CoverageLineGlyphTag>,IListener<CoverageColoursChangedMessage>
	{
        private ICoverageColours coverageColours;

        public CoverageLineGlyphTagger(
			ITextBuffer textBuffer, 
			FileLineCoverage lastCoverageLines, 
			IEventAggregator eventAggregator,
			ICoverageColours coverageColours,
            ICoverageTypeFilter coverageTypeFilter

        ) : base(textBuffer, lastCoverageLines, coverageTypeFilter, eventAggregator)
		{
			this.coverageColours = coverageColours;
        }

        protected override TagSpan<CoverageLineGlyphTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
        {
            span = base.GetLineSnapshotSpan(coverageLine.Number, span);
            var colour = coverageColours.GetColour(coverageLine.CoverageType).Background;
            return new TagSpan<CoverageLineGlyphTag>(span, new CoverageLineGlyphTag(coverageLine,colour));
        }

        public void Handle(CoverageColoursChangedMessage message)
        {
            this.coverageColours = message.CoverageColours;
            RaiseTagsChanged();
        }
    }
}