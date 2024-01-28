using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTagger : CoverageLineTaggerBase<CoverageLineGlyphTag>, IListener<CoverageColoursChangedMessage>
	{
        private ICoverageColours coverageColours;
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        public CoverageLineGlyphTagger(
			ITextBuffer textBuffer, 
			FileLineCoverage lastCoverageLines, 
			IEventAggregator eventAggregator,
			ICoverageColours coverageColours,
            ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper

        ) : base(textBuffer, lastCoverageLines, eventAggregator)
		{
			this.coverageColours = coverageColours;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        public void Handle(CoverageColoursChangedMessage message)
        {
            this.coverageColours = message.CoverageColours;
			RaiseTagsChanged();
        }

        protected override TagSpan<CoverageLineGlyphTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
        {
            var coverageType = coverageLineCoverageTypeInfoHelper.GetInfo(coverageLine).CoverageType;

            var colour = coverageColours.GetColor(coverageType).Background;
			return new TagSpan<CoverageLineGlyphTag>(span, new CoverageLineGlyphTag(coverageLine,colour));
        }
    }
}