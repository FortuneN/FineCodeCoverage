using System.Collections.Generic;
using FineCodeCoverage.Editor.DynamicCoverage;

namespace FineCodeCoverage.Editor.Management
{
    internal interface IFontAndColorsInfosProvider
    {
        Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos();
        Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetFontAndColorsInfos();
        ICoverageFontAndColorsCategoryItemNames CoverageFontAndColorsCategoryItemNames { set; }
    }
}
