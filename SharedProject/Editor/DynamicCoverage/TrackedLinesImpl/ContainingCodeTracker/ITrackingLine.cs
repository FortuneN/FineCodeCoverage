using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingLine
    {
        IDynamicLine Line { get; }

        bool Update(ITextSnapshot currentSnapshot);
    }
}
