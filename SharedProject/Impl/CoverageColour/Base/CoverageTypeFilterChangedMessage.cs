namespace FineCodeCoverage.Impl
{
    internal class CoverageTypeFilterChangedMessage
    {
        public CoverageTypeFilterChangedMessage(ICoverageTypeFilter filter)
        {
            Filter = filter;
        }

        public ICoverageTypeFilter Filter { get; }
    }
}
