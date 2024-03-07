namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDynamicLine
    {
        int Number { get; }
        DynamicCoverageType CoverageType { get; }
    }
}
