using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface IFontsAndColorsHelper
    {
        System.Threading.Tasks.Task<List<IItemCoverageColours>> GetColorsAsync(Guid category, IEnumerable<string> names);
    }

}
