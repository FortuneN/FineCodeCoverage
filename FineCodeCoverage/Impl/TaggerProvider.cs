using System.ComponentModel.Composition;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

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