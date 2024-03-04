using Microsoft.CodeAnalysis;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal class RootNodeAndLanguage
    {
        public SyntaxNode Root { get; }
        public string Language { get; }

        public RootNodeAndLanguage(SyntaxNode root, string language)
        {
            this.Root = root;
            this.Language = language;
        }
    }
}
