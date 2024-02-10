using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface ICoverageClassificationColourService : ICoverageTypeService
    {
        void SetCoverageColours(IEnumerable<ICoverageTypeColour> coverageTypeColours);
    }
}
