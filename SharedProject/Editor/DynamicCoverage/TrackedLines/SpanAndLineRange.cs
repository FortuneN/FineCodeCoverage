using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class SpanAndLineRange
    {
        public SpanAndLineRange(Span span, int startLineNumber, int endLineNumber)
        {
            this.Span = span;
            this.StartLineNumber = startLineNumber;
            this.EndLineNumber = endLineNumber;
        }

        public Span Span { get; }
        public int StartLineNumber { get; }
        public int EndLineNumber { get; }

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
            => obj is SpanAndLineRange other &&
            other.Span.Equals(this.Span) && other.StartLineNumber == this.StartLineNumber && other.EndLineNumber == this.EndLineNumber;

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = -414942;
            hashCode = (hashCode * -1521134295) + this.Span.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.StartLineNumber.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.EndLineNumber.GetHashCode();
            return hashCode;
        }
    }
}
