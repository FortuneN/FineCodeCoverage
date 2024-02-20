namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DynamicLine : IDynamicLine
    {
        public int ActualLineNumber { get; set; }
        public DynamicLine(int actualLineNumber, DynamicCoverageType dynamicCoverageType)
        {
            ActualLineNumber = actualLineNumber;
            CoverageType = dynamicCoverageType;
        }
        public int Number => ActualLineNumber + 1;

        public DynamicCoverageType CoverageType { get; }
    }
}
