namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn
{
    internal interface IRoslynFileCodeSpanRangeService
    {
        IFileCodeSpanRangeService FileCodeSpanRangeService { get; }
        IFileCodeSpanRangeService FileCodeSpanRangeServiceForChanges { get; }
    }
}
