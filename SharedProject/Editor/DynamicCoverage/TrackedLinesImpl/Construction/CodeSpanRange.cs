using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CodeSpanRange
    {
        public CodeSpanRange(int startLine, int endLine)
        {
            this.StartLine = startLine;
            this.EndLine = endLine;
        }
        public static CodeSpanRange SingleLine(int lineNumber) => new CodeSpanRange(lineNumber, lineNumber);
        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public override bool Equals(object obj)
            => obj is CodeSpanRange codeSpanRange && codeSpanRange.StartLine == this.StartLine && codeSpanRange.EndLine == this.EndLine;

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = -1763436595;
            hashCode = (hashCode * -1521134295) + this.StartLine.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.EndLine.GetHashCode();
            return hashCode;
        }
    }
}
