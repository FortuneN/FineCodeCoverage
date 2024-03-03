using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ICoverageLine
    {
        bool Update(ITextSnapshot currentSnapshot);
        IDynamicLine Line { get; }
    }
}
