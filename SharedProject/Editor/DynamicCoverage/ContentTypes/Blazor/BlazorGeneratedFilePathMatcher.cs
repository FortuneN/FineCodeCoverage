using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IBlazorGeneratedFilePathMatcher))]
    internal class BlazorGeneratedFilePathMatcher : IBlazorGeneratedFilePathMatcher
    {
        public bool IsBlazorGeneratedFilePath(string razorFilePath, string generatedFilePath)
            => generatedFilePath.StartsWith($"{razorFilePath}.");
    }
}
