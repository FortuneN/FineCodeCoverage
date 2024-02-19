using Microsoft.VisualStudio.Text;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class SpanAndLineRange
    {
        public SpanAndLineRange(Span span, int startLineNumber, int endLineNumber)
        {
            Span = span;
            StartLineNumber = startLineNumber;
            EndLineNumber = endLineNumber;
        }

        public Span Span { get; }
        public int StartLineNumber { get; }
        public int EndLineNumber { get; }

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            var other = obj as SpanAndLineRange;
            return other != null && other.Span.Equals(Span) && other.StartLineNumber == StartLineNumber && other.EndLineNumber == EndLineNumber;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = -414942;
            hashCode = hashCode * -1521134295 + Span.GetHashCode();
            hashCode = hashCode * -1521134295 + StartLineNumber.GetHashCode();
            hashCode = hashCode * -1521134295 + EndLineNumber.GetHashCode();
            return hashCode;
        }
    }
}
