namespace FineCodeCoverage.Impl
{
    internal class CoverageMarginOptionsChangedMessage
    {
        public CoverageMarginOptionsChangedMessage(ICoverageMarginOptions options)
        {
            Options = options;
        }
        public ICoverageMarginOptions Options { get; }
    }

}
