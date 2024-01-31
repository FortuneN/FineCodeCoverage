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
			IFileLineCoverage lastCoverageLines, 
			IEventAggregator eventAggregator,
			ICoverageColours coverageColours,
            ICoverageTypeFilter coverageTypeFilter,
            ILineSpanLogic lineSpanLogic

        ) : base(textBuffer, lastCoverageLines, coverageTypeFilter, eventAggregator,lineSpanLogic)
		{
			this.coverageColours = coverageColours;
        }

        protected override TagSpan<CoverageLineGlyphTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
        {
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