using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface ILanguageContainingCodeVisitor
    {
        List<TextSpan> GetSpans(SyntaxNode rootNode);
    }
}
