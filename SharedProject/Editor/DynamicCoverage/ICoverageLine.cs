using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ICoverageLine
    {
        CoverageLineUpdateType Update(ITextSnapshot currentSnapshot);
        IDynamicLine Line { get; }
    }
}
