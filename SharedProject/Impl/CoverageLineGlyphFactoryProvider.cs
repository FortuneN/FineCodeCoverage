using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Order(Before = "VsTextMarker")]
    [Name(Vsix.GlyphFactoryProviderName)]
    [Export(typeof(IGlyphFactoryProvider))]
    internal class CoverageLineGlyphFactoryProvider : IGlyphFactoryProvider
    {
        private readonly ICoverageColours coverageColours;

        [ImportingConstructor]
        public CoverageLineGlyphFactoryProvider(ICoverageColours coverageColours)
        {
            this.coverageColours = coverageColours;
        }
        public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
        {
            return new CoverageLineGlyphFactory(coverageColours);
        }
    }
}