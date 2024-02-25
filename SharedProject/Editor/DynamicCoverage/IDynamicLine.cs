namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IDynamicLine
    {
        int Number { get; }
        DynamicCoverageType CoverageType { get; }
    }
}
