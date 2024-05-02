namespace FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction
{
    internal interface ICoverageContentType
    {
        string ContentTypeName { get; }
        IFileCodeSpanRangeService FileCodeSpanRangeService { get; }
        IFileCodeSpanRangeService FileCodeSpanRangeServiceForChanges { get; }
        ILineExcluder LineExcluder { get; }
    }
}
