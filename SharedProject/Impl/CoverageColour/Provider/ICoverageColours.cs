using System;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageColours
    {
        IItemCoverageColours GetColor(CoverageType coverageType);
    }

    internal interface ICoverageColoursProvider
    {
        ICoverageColours GetCoverageColours();
    }
}