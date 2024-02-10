namespace FineCodeCoverage.Editor.Roslyn
{
    interface ILanguageContainingCodeVisitorFactory
    {
        ILanguageContainingCodeVisitor Create(bool isCSharp);
    }
}
