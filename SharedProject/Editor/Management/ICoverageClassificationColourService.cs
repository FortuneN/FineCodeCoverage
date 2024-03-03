using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageClassificationColourService : ICoverageTypeService
    {
        void SetCoverageColours(IEnumerable<ICoverageTypeColour> coverageTypeColours);
    }
}
