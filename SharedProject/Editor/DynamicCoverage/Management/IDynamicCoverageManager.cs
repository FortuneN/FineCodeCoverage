namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDynamicCoverageManager
    {
        IBufferLineCoverage Manage(ITextInfo textInfo);
    }
}
