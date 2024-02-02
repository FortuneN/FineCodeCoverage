using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
    class CoverageTypeColour : ICoverageTypeColour
    {
        public CoverageTypeColour(CoverageType coverageType, TextFormattingRunProperties textFormattingRunProperties)
        {
            CoverageType = coverageType;
            TextFormattingRunProperties = textFormattingRunProperties;
        }

        public CoverageType CoverageType { get; }
        public TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
