using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ICSharpCodeCoverageNodeVisitor
    {
        List<SyntaxNode> GetNodes(SyntaxNode rootNode);
    }
}
