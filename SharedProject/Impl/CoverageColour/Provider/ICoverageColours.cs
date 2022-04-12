using System;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageColours
    {
        event EventHandler<EventArgs> ColoursChanged;
        System.Windows.Media.Color CoverageTouchedArea { get; }
        System.Windows.Media.Color CoverageNotTouchedArea { get; }
        System.Windows.Media.Color CoveragePartiallyTouchedArea { get; }
    }

}