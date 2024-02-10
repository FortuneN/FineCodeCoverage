using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    interface ICoverageTypeColour
    {
        CoverageType CoverageType { get; }
        TextFormattingRunProperties TextFormattingRunProperties { get; }
    }
}
