using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    interface IFontAndColorsInfosProvider
    {
        Dictionary<CoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos();
        Dictionary<CoverageType, IFontAndColorsInfo> GetFontAndColorsInfos();
    }
}
