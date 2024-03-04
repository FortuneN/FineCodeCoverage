using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ILanguageContainingCodeVisitor
    {
        List<TextSpan> GetSpans(SyntaxNode rootNode);
    }
}
