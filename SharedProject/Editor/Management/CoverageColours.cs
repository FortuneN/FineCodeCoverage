using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    internal class CoverageColours : ICoverageColours
    {
        public IFontAndColorsInfo CoverageTouchedInfo { get; }
        public IFontAndColorsInfo CoverageNotTouchedInfo { get; }
        public IFontAndColorsInfo CoveragePartiallyTouchedInfo { get; }
        private readonly Dictionary<CoverageType, IFontAndColorsInfo> coverageTypeToFontAndColorsInfo;
        public CoverageColours(
            IFontAndColorsInfo coverageTouchedColors,
            IFontAndColorsInfo coverageNotTouched,
            IFontAndColorsInfo coveragePartiallyTouchedColors
        )
        {
            CoverageTouchedInfo = coverageTouchedColors;
            CoverageNotTouchedInfo = coverageNotTouched;
            CoveragePartiallyTouchedInfo = coveragePartiallyTouchedColors;
            coverageTypeToFontAndColorsInfo = new Dictionary<CoverageType, IFontAndColorsInfo>
            {
                { CoverageType.Covered, coverageTouchedColors},
                {CoverageType.NotCovered, coverageNotTouched },
                { CoverageType.Partial, coveragePartiallyTouchedColors}
            };
        }

        internal Dictionary<CoverageType, IFontAndColorsInfo> GetChanges(CoverageColours lastCoverageColours)
        {
            var changes = new Dictionary<CoverageType, IFontAndColorsInfo>();
            if (lastCoverageColours == null) return new Dictionary<CoverageType, IFontAndColorsInfo>
            {
                { CoverageType.Covered, CoverageTouchedInfo},
                {CoverageType.NotCovered, CoverageNotTouchedInfo },
                { CoverageType.Partial, CoveragePartiallyTouchedInfo}
            };

            if (!CoverageTouchedInfo.Equals(lastCoverageColours.CoverageTouchedInfo))
            {
                changes.Add(CoverageType.Covered, CoverageTouchedInfo);
            }
            if (!CoverageNotTouchedInfo.Equals(lastCoverageColours.CoverageNotTouchedInfo))
            {
                changes.Add(CoverageType.NotCovered, CoverageNotTouchedInfo);
            }
            if (!CoveragePartiallyTouchedInfo.Equals(lastCoverageColours.CoveragePartiallyTouchedInfo))
            {
                changes.Add(CoverageType.Partial, CoveragePartiallyTouchedInfo);
            }
            return changes;
        }

        public IItemCoverageColours GetColour(CoverageType coverageType)
        {
            return coverageTypeToFontAndColorsInfo[coverageType].ItemCoverageColours;
        }
    }

}
