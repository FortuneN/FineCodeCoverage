namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ICodeLineExcluder
    {
        bool ExcludeIfNotCode(string text, bool isCSharp);
    }
}
