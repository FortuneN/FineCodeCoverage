namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IDynamicCoverageManager
    {
        IBufferLineCoverage Manage(ITextInfo textInfo);
    }
}
