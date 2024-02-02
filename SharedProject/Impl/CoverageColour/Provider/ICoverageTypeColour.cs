using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
    interface ICoverageTypeColour
    {
        CoverageType CoverageType { get; }
        TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
