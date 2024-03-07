using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextInfoFactory
    {
        ITextInfo Create(ITextView textView, ITextBuffer textBuffer);
    }
}
