using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(GlyphTag))]
    [Name(Vsix.TaggerProviderName)]
    [Export(typeof(ITaggerProvider))]
    internal class TaggerProvider : ITaggerProvider
    {
        private readonly IFCCEngine fccEngine;

        [ImportingConstructor]
        public TaggerProvider(IFCCEngine fccEngine)
        {
            this.fccEngine = fccEngine;
        }
        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            return new Tagger<T>(textBuffer, fccEngine);
        }
    }
}