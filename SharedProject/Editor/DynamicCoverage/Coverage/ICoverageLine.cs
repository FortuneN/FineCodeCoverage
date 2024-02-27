using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ICoverageLine
    {
        bool Update(ITextSnapshot currentSnapshot);
        IDynamicLine Line { get; }
    }
}
