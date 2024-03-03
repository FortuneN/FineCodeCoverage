using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    internal interface IFontsAndColorsHelper
    {
        System.Threading.Tasks.Task<List<IFontAndColorsInfo>> GetInfosAsync(Guid category, IEnumerable<string> names);
    }
}