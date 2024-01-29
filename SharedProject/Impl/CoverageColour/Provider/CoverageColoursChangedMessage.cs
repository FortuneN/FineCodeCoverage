namespace FineCodeCoverage.Impl
{
    internal class CoverageColoursChangedMessage
    {
        public ICoverageColours CoverageColours { get; }

        public CoverageColoursChangedMessage(ICoverageColours currentCoverageColours)
        {
            this.CoverageColours = currentCoverageColours;
        }
    }
}
