namespace FineCodeCoverage.Impl
{
    internal interface ICoverageColours
    {

        System.Windows.Media.Color CoverageTouchedArea { get; }
        System.Windows.Media.Color CoverageNotTouchedArea { get; }
        System.Windows.Media.Color CoveragePartiallyTouchedArea { get; }
    }

}