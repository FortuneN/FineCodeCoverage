using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
    [Export(typeof(ITaggerProvider))]
    internal class CoverageLineGlyphTaggerProvider : ITaggerProvider
    {
        private readonly IFCCEngine fccEngine;
        private readonly ICoverageColoursProvider coverageColoursProvider;

        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(IFCCEngine fccEngine, ICoverageColoursProvider coverageColoursProvider)
        {
            this.fccEngine = fccEngine;
            this.coverageColoursProvider = coverageColoursProvider;
        }
        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            return new CoverageLineGlyphTagger(textBuffer, fccEngine, coverageColoursProvider) as ITagger<T>;
        }
    }
}