using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextInfoFactory
    {
        ITextInfo Create(ITextView textView, ITextBuffer textBuffer);
    }
}
