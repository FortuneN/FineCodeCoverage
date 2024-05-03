using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IRazorGeneratedFilePathMatcher))]
    internal class RazorGeneratedFilePathMatcher : IRazorGeneratedFilePathMatcher
    {
        public bool IsRazorGeneratedFilePath(string razorFilePath, string generatedFilePath)
            => generatedFilePath.StartsWith($"{razorFilePath}.");
    }
}
