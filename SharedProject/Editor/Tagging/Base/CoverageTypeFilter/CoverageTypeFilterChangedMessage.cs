namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal class CoverageTypeFilterChangedMessage
    {
        public CoverageTypeFilterChangedMessage(ICoverageTypeFilter filter) => this.Filter = filter;

        public ICoverageTypeFilter Filter { get; }
    }
}
