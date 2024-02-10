using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Roslyn
{
    interface ILanguageContainingCodeVisitor
    {
        List<TextSpan> GetSpans(SyntaxNode rootNode);
    }
}
