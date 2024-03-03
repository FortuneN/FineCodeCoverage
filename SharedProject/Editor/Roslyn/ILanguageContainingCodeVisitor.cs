using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    interface ILanguageContainingCodeVisitor
    {
        List<TextSpan> GetSpans(SyntaxNode rootNode);
    }
}
