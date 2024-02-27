using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDirtyLine
    {
        IDynamicLine Line { get; }

        bool Update(ITextSnapshot currentSnapshot);
    }
}
