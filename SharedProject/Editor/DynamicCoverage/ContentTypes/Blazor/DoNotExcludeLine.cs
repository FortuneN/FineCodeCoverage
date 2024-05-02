namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    internal class DoNotExcludeLine : ILineExcluder
    {
        public bool ExcludeIfNotCode(string text) => false;
    }
}
