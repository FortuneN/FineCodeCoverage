using FineCodeCoverage.Editor.DynamicCoverage;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    interface IFontAndColorsInfosProvider
    {
        Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos();
        Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetFontAndColorsInfos();
        ICoverageFontAndColorsCategoryItemNames CoverageFontAndColorsCategoryItemNames { set; }
    }
}
