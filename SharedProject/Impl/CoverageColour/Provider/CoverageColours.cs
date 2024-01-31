using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class CoverageColours : ICoverageColours
    {
        public IFontsAndColorsInfo CoverageTouchedInfo { get; }
        public IFontsAndColorsInfo CoverageNotTouchedInfo { get; }
        public IFontsAndColorsInfo CoveragePartiallyTouchedInfo { get; }
        public CoverageColours(
            IFontsAndColorsInfo coverageTouchedColors,
            IFontsAndColorsInfo coverageNotTouched,
            IFontsAndColorsInfo coveragePartiallyTouchedColors
        )
        {
            CoverageTouchedInfo = coverageTouchedColors;
            CoverageNotTouchedInfo = coverageNotTouched;
            CoveragePartiallyTouchedInfo = coveragePartiallyTouchedColors;
        }

        internal Dictionary<CoverageType, IFontsAndColorsInfo> GetChanges(CoverageColours lastCoverageColours)
        {
            var changes = new Dictionary<CoverageType, IFontsAndColorsInfo>();
            if (lastCoverageColours == null) return new Dictionary<CoverageType, IFontsAndColorsInfo>
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
            switch (coverageType)
            {
                case CoverageType.Partial:
                    return CoveragePartiallyTouchedInfo.ItemCoverageColours;
                case CoverageType.NotCovered:
                    return CoverageNotTouchedInfo.ItemCoverageColours;
                case CoverageType.Covered:
                    return CoverageTouchedInfo.ItemCoverageColours;
            }
            return default;
        }

    }

}
