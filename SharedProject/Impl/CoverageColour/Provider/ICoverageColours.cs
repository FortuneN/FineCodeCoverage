using System;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageColours
    {
        System.Windows.Media.Color GetColor(CoverageType coverageType);
        //System.Windows.Media.Color CoverageTouchedArea { get; }
        //System.Windows.Media.Color CoverageNotTouchedArea { get; }
        //System.Windows.Media.Color CoveragePartiallyTouchedArea { get; }
    }

    internal interface ICoverageColoursProvider
    {
        ICoverageColours GetCoverageColours();
    }
}