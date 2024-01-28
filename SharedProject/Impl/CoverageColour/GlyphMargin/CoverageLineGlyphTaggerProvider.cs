using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Classification;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineGlyphTagger, CoverageLineGlyphTag>
    {
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageColoursProvider coverageColoursProvider,
            ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
        ) : base(eventAggregator)
        {
            this.coverageColoursProvider = coverageColoursProvider;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }


        protected override CoverageLineGlyphTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator)
        {
            return new CoverageLineGlyphTagger(textBuffer, lastCoverageLines,eventAggregator,coverageColoursProvider.GetCoverageColours(), coverageLineCoverageTypeInfoHelper);
        }
    }
}