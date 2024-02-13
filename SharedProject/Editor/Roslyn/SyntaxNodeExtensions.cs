using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal static class SyntaxNodeExtensions
    {
        public static TextSpan GetLeadingNoTrailingSpan(this SyntaxNode node)
        {
            var fullSpan = node.FullSpan;
            var start = fullSpan.Start;
            var trailingFullSpan = node.GetTrailingTrivia().FullSpan;
            return new TextSpan(start, fullSpan.Length - trailingFullSpan.Length);
        }
    }
}
