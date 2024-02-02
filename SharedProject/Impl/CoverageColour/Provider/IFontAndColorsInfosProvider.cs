using FineCodeCoverage.Impl;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IFontAndColorsInfosProvider
    {
        Dictionary<CoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos();
        Dictionary<CoverageType, IFontAndColorsInfo> GetFontAndColorsInfos();
    }
}
