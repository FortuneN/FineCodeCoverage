using FineCodeCoverage.Editor.DynamicCoverage;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageColours
    {
        IItemCoverageColours GetColour(DynamicCoverageType coverageType);
    }
}