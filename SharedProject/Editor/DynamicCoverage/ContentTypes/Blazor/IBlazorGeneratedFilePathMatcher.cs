namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    interface IBlazorGeneratedFilePathMatcher
    {
        bool IsBlazorGeneratedFilePath(string razorFilePath, string generatedfilePath);
    }
}
