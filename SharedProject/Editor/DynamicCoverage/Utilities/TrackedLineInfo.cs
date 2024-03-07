namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal readonly struct TrackedLineInfo
    {
        public TrackedLineInfo(int lineNumber, string lineText)
        {
            this.LineNumber = lineNumber;
            this.LineText = lineText;
        }
        public int LineNumber { get; }
        public string LineText { get; }
    }
}
