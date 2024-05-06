using Microsoft.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage.Utilities
{
    internal interface ISyntaxNodeLocationMapper
    {
        FileLinePositionSpan Map(SyntaxNode node);
    }
}
