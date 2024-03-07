using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextInfo
    {
        string FilePath { get; }
        ITextBuffer2 TextBuffer { get; }
        ITextView TextView { get; }
    }
}
