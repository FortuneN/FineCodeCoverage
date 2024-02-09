namespace FineCodeCoverage.Impl
{
    internal class CodeSpanRange
    {
        public CodeSpanRange(int startLine, int endLine)
        {
            StartLine = startLine;
            EndLine = endLine;
        }
        public int StartLine { get; set; }
        public int EndLine { get; set; }

    }
}
