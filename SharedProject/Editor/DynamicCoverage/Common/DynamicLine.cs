namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DynamicLine : IDynamicLine
    {
        public DynamicLine(int lineNumber, DynamicCoverageType dynamicCoverageType)
        {
            this.Number = lineNumber;
            this.CoverageType = dynamicCoverageType;
        }

        public int Number { get; set; }

        public DynamicCoverageType CoverageType { get; set; }
    }
}
