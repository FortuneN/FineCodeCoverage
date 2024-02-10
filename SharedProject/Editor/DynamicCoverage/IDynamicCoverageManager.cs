using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IDynamicCoverageManager
    {
        IBufferLineCoverage Manage(ITextView textView, ITextBuffer buffer, string filePath);
    }
}
