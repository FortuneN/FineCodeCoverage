using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using OrderAttribute = Microsoft.VisualStudio.Utilities.OrderAttribute;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    [ExcludeFromCodeCoverage]
    [ContentType(SupportedContentTypeLanguages.CSharp)]
    [ContentType(SupportedContentTypeLanguages.VisualBasic)]
    [ContentType(SupportedContentTypeLanguages.CPP)]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Order(Before = "VsTextMarker")]
    [Name(Vsix.GlyphFactoryProviderName)]
    [Export(typeof(IGlyphFactoryProvider))]
    internal class CoverageLineGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin) => new CoverageLineGlyphFactory();

    }
}