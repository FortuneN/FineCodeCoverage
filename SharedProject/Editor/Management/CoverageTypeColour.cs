using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
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
