namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal enum DynamicCoverageType
    {
        Covered, Partial, NotCovered,
        Dirty,
        NewLine
    }
    interface IDynamicLine
    {
        int Number { get; }
        DynamicCoverageType CoverageType { get; }
    }
}
