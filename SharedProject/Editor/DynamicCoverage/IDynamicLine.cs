namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal enum DynamicCoverageType
    {
        Covered, Partial, NotCovered,
        CoveredDirty, PartialDirty, NotCoveredDirty,
        NewLine
    }
    interface IDynamicLine
    {
        int Number { get; }
        DynamicCoverageType CoverageType { get; }
    }
}
