namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DynamicLine : IDynamicLine
    {
        public DynamicLine(int lineNumber, DynamicCoverageType dynamicCoverageType)
        {
            Number = lineNumber;
            CoverageType = dynamicCoverageType;
        }
        public int Number { get; set; }

        public DynamicCoverageType CoverageType { get; }
    }
}
