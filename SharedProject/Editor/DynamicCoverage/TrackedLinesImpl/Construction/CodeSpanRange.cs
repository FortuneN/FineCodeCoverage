using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CodeSpanRange
    {
        public CodeSpanRange(int startLine, int endLine)
        {
            StartLine = startLine;
            EndLine = endLine;
        }
        public static CodeSpanRange SingleLine(int lineNumber)
        {
            return new CodeSpanRange(lineNumber, lineNumber);
        }
        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CodeSpanRange codeSpanRange && codeSpanRange.StartLine == StartLine && codeSpanRange.EndLine == EndLine;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = -1763436595;
            hashCode = hashCode * -1521134295 + StartLine.GetHashCode();
            hashCode = hashCode * -1521134295 + EndLine.GetHashCode();
            return hashCode;
        }
    }
}
