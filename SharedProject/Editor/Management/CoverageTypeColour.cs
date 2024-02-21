using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    class CoverageTypeColour : ICoverageTypeColour
    {
        public CoverageTypeColour(DynamicCoverageType coverageType, TextFormattingRunProperties textFormattingRunProperties)
        {
            CoverageType = coverageType;
            TextFormattingRunProperties = textFormattingRunProperties;
        }

        public DynamicCoverageType CoverageType { get; }
        public TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
