namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    interface IRazorGeneratedFilePathMatcher
    {
        bool IsRazorGeneratedFilePath(string razorFilePath, string generatedfilePath);
    }
}
