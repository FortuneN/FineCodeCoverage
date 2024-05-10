namespace FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction
{
    internal interface ICoverageContentType
    {
        string ContentTypeName { get; }
        IFileCodeSpanRangeService FileCodeSpanRangeService { get; }
        bool CoverageOnlyFromFileCodeSpanRangeService { get; }
        bool UseFileCodeSpanRangeServiceForChanges { get; }
        ILineExcluder LineExcluder { get; }
    }
}
