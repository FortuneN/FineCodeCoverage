using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextInfo
    {
        string FilePath { get; }
        ITextBuffer2 TextBuffer { get; }
        ITextView TextView { get; }
    }
}
