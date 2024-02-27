namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageChangedMessage
    {
        public IBufferLineCoverage CoverageLines { get; }
        public string AppliesTo { get; }

        public CoverageChangedMessage(IBufferLineCoverage coverageLines, string appliesTo)
        {
            CoverageLines = coverageLines;
            AppliesTo = appliesTo;
        }
    }
}
