using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageColours
    {
        IItemCoverageColours GetColour(CoverageType coverageType);
    }
}