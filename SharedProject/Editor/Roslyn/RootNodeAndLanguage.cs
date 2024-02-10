using Microsoft.CodeAnalysis;

namespace FineCodeCoverage.Editor.Roslyn
{
    class RootNodeAndLanguage
    {
        public SyntaxNode Root { get; }
        public string Language { get; }

        public RootNodeAndLanguage(SyntaxNode root, string language)
        {
            Root = root;
            Language = language;
        }
    }
}
