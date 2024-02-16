namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DirtyDynamicLine : IDynamicLine
    {
        public DirtyDynamicLine(int number)
        {
            Number = number;
        }
        public int Number { get; }

        public DynamicCoverageType CoverageType => DynamicCoverageType.Dirty;
    }

}
