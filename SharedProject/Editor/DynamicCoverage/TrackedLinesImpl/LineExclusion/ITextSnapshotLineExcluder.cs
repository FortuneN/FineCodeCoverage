using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITextSnapshotLineExcluder
    {
        bool ExcludeIfNotCode(ITextSnapshot textSnapshot, int lineNumber, bool isCSharp);
    }
}
