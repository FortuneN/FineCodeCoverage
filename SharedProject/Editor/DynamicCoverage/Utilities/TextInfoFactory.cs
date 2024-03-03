using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITextInfoFactory))]
    internal class TextInfoFactory : ITextInfoFactory
    {
        public ITextInfo Create(ITextView textView, ITextBuffer textBuffer) => new TextInfo(textView, textBuffer);
    }
}
