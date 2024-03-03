using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.Roslyn
{
    [Export(typeof(ILanguageContainingCodeVisitorFactory))]
    internal class LanguageContainingCodeVisitorFactory : ILanguageContainingCodeVisitorFactory
    {
        public ILanguageContainingCodeVisitor Create(bool isCSharp) 
            => isCSharp ? new CSharpContainingCodeVisitor() as ILanguageContainingCodeVisitor : new VBContainingCodeVisitor();
    }
}
