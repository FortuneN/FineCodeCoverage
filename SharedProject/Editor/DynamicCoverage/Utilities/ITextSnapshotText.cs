using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextSnapshotText
    {
        string GetLineText(ITextSnapshot textSnapshot, int lineNumber);
    }
}
