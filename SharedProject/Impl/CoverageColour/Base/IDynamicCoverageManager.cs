using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface IDynamicCoverageManager
    {
        IBufferLineCoverage Manage(ITextView textView, ITextBuffer buffer, string filePath);
    }
}
