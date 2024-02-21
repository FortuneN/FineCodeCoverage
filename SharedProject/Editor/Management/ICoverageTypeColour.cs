using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    interface ICoverageTypeColour
    {
        DynamicCoverageType CoverageType { get; }
        TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
