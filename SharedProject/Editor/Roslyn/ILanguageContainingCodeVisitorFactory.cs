namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ILanguageContainingCodeVisitorFactory
    {
        ILanguageContainingCodeVisitor Create(bool isCSharp);
    }
}
