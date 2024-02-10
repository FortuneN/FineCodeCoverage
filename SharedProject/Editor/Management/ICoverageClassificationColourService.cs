using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    interface ICoverageClassificationColourService : ICoverageTypeService
    {
        void SetCoverageColours(IEnumerable<ICoverageTypeColour> coverageTypeColours);
    }
}
