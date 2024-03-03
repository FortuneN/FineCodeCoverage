using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackedNewCodeLine
    {
        TrackedNewCodeLineUpdate Update(ITextSnapshot currentSnapshot);
        string GetText(ITextSnapshot currentSnapshot);
        IDynamicLine Line { get; }
    }
}
