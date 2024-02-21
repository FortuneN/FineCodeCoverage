using FineCodeCoverage.Editor.DynamicCoverage;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    internal class CoverageColours : ICoverageColours
    {
        public IFontAndColorsInfo CoverageTouchedInfo { get; }
        public IFontAndColorsInfo CoverageNotTouchedInfo { get; }
        public IFontAndColorsInfo CoveragePartiallyTouchedInfo { get; }
        public IFontAndColorsInfo DirtyInfo { get; }
        public IFontAndColorsInfo NewLineInfo { get; }

        private readonly Dictionary<DynamicCoverageType, IFontAndColorsInfo> coverageTypeToFontAndColorsInfo;
        public CoverageColours(
            IFontAndColorsInfo coverageTouchedInfo,
            IFontAndColorsInfo coverageNotTouchedInfo,
            IFontAndColorsInfo coveragePartiallyTouchedInfo,
            IFontAndColorsInfo dirtyInfo,
            IFontAndColorsInfo newLineInfo
        )
        {
            CoverageTouchedInfo = coverageTouchedInfo;
            CoverageNotTouchedInfo = coverageNotTouchedInfo;
            CoveragePartiallyTouchedInfo = coveragePartiallyTouchedInfo;
            DirtyInfo = dirtyInfo;
            NewLineInfo = newLineInfo;
            coverageTypeToFontAndColorsInfo = new Dictionary<DynamicCoverageType, IFontAndColorsInfo>
            {
                { DynamicCoverageType.Covered, coverageTouchedInfo},
                { DynamicCoverageType.NotCovered, coverageNotTouchedInfo },
                { DynamicCoverageType.Partial, coveragePartiallyTouchedInfo},
                { DynamicCoverageType.Dirty, dirtyInfo},
                { DynamicCoverageType.NewLine, newLineInfo}
            };
        }

        internal Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChanges(CoverageColours lastCoverageColours)
        {
            var changes = new Dictionary<DynamicCoverageType, IFontAndColorsInfo>();
            if (lastCoverageColours == null) return new Dictionary<DynamicCoverageType, IFontAndColorsInfo>
            {
                { DynamicCoverageType.Covered, CoverageTouchedInfo},
                { DynamicCoverageType.NotCovered, CoverageNotTouchedInfo },
                { DynamicCoverageType.Partial, CoveragePartiallyTouchedInfo},
                { DynamicCoverageType.Dirty, DirtyInfo},
                { DynamicCoverageType.NewLine, NewLineInfo}
            };
                
            if (!CoverageTouchedInfo.Equals(lastCoverageColours.CoverageTouchedInfo))
            {
                changes.Add(DynamicCoverageType.Covered, CoverageTouchedInfo);
            }
            if (!CoverageNotTouchedInfo.Equals(lastCoverageColours.CoverageNotTouchedInfo))
            {
                changes.Add(DynamicCoverageType.NotCovered, CoverageNotTouchedInfo);
            }
            if (!CoveragePartiallyTouchedInfo.Equals(lastCoverageColours.CoveragePartiallyTouchedInfo))
            {
                changes.Add(DynamicCoverageType.Partial, CoveragePartiallyTouchedInfo);
            }
            if (!DirtyInfo.Equals(lastCoverageColours.DirtyInfo))
            {
                changes.Add(DynamicCoverageType.Dirty, DirtyInfo);
            }
            if (!NewLineInfo.Equals(lastCoverageColours.NewLineInfo))
            {
                changes.Add(DynamicCoverageType.NewLine, NewLineInfo);
            }
            return changes;
        }

        public IItemCoverageColours GetColour(DynamicCoverageType coverageType)
        {
            return coverageTypeToFontAndColorsInfo[coverageType].ItemCoverageColours;
        }
    }

}
