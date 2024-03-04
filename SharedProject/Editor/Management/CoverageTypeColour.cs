using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    internal class CoverageTypeColour : ICoverageTypeColour
    {
        public CoverageTypeColour(DynamicCoverageType coverageType, TextFormattingRunProperties textFormattingRunProperties)
        {
            this.CoverageType = coverageType;
            this.TextFormattingRunProperties = textFormattingRunProperties;
        }

        public DynamicCoverageType CoverageType { get; }
        public TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
