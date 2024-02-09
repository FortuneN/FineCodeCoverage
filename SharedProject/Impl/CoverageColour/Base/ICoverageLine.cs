using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface ICoverageLine
    {
        CoverageLineUpdateType Update(ITextSnapshot currentSnapshot);
        void Dirty();
        IDynamicLine Line { get; }
    }
}
