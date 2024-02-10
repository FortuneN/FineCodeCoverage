using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ICoverageLine
    {
        CoverageLineUpdateType Update(ITextSnapshot currentSnapshot);
        void Dirty();
        IDynamicLine Line { get; }
    }
}
