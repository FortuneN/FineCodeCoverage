using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface IFontsAndColorsHelper
    {
        System.Threading.Tasks.Task<List<IFontAndColorsInfo>> GetInfosAsync(Guid category, IEnumerable<string> names);
    }

}
