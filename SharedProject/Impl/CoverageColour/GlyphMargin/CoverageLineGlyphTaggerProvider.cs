using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineGlyphTagger, CoverageLineGlyphTag>
    {
        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator
        ) : base(eventAggregator) { }
        

        protected override CoverageLineGlyphTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines)
        {
            return new CoverageLineGlyphTagger(textBuffer, lastCoverageLines);
        }
    }
}