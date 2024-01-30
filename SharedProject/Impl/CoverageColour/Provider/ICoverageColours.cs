namespace FineCodeCoverage.Impl
{
    internal interface ICoverageColours
    {
        IItemCoverageColours GetColour(CoverageType coverageType);
    }
}