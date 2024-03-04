using System;
using System.Collections.Generic;
using FineCodeCoverage.Editor.DynamicCoverage;

namespace FineCodeCoverage.Editor.Management
{
    internal class CoverageColours : ICoverageColours
    {
        public IFontAndColorsInfo CoverageTouchedInfo { get; }
        public IFontAndColorsInfo CoverageNotTouchedInfo { get; }
        public IFontAndColorsInfo CoveragePartiallyTouchedInfo { get; }
        public IFontAndColorsInfo DirtyInfo { get; }
        public IFontAndColorsInfo NewLineInfo { get; }
        public IFontAndColorsInfo NotIncludedInfo { get; }

        private readonly Dictionary<DynamicCoverageType, IFontAndColorsInfo> coverageTypeToFontAndColorsInfo;
        public CoverageColours(
            IFontAndColorsInfo coverageTouchedInfo,
            IFontAndColorsInfo coverageNotTouchedInfo,
            IFontAndColorsInfo coveragePartiallyTouchedInfo,
            IFontAndColorsInfo dirtyInfo,
            IFontAndColorsInfo newLineInfo,
            IFontAndColorsInfo notIncludedInfo
        )
        {
            this.CoverageTouchedInfo = coverageTouchedInfo;
            this.CoverageNotTouchedInfo = coverageNotTouchedInfo;
            this.CoveragePartiallyTouchedInfo = coveragePartiallyTouchedInfo;
            this.DirtyInfo = dirtyInfo;
            this.NewLineInfo = newLineInfo;
            this.NotIncludedInfo = notIncludedInfo;
            this.coverageTypeToFontAndColorsInfo = new Dictionary<DynamicCoverageType, IFontAndColorsInfo>
            {
                { DynamicCoverageType.Covered, coverageTouchedInfo},
                { DynamicCoverageType.NotCovered, coverageNotTouchedInfo },
                { DynamicCoverageType.Partial, coveragePartiallyTouchedInfo},
                { DynamicCoverageType.Dirty, dirtyInfo},
                { DynamicCoverageType.NewLine, newLineInfo},
                { DynamicCoverageType.NotIncluded, notIncludedInfo}
            };
        }

        internal Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChanges(CoverageColours lastCoverageColours)
        {
            var changes = new Dictionary<DynamicCoverageType, IFontAndColorsInfo>();
            if (lastCoverageColours == null)
            {
                return new Dictionary<DynamicCoverageType, IFontAndColorsInfo>
                {
                    { DynamicCoverageType.Covered, this.CoverageTouchedInfo},
                    { DynamicCoverageType.NotCovered, this.CoverageNotTouchedInfo },
                    { DynamicCoverageType.Partial, this.CoveragePartiallyTouchedInfo},
                    { DynamicCoverageType.Dirty, this.DirtyInfo},
                    { DynamicCoverageType.NewLine, this.NewLineInfo},
                    { DynamicCoverageType.NotIncluded, this.NotIncludedInfo}
                };
            }

            if (!this.CoverageTouchedInfo.Equals(lastCoverageColours.CoverageTouchedInfo))
            {
                changes.Add(DynamicCoverageType.Covered, this.CoverageTouchedInfo);
            }

            if (!this.CoverageNotTouchedInfo.Equals(lastCoverageColours.CoverageNotTouchedInfo))
            {
                changes.Add(DynamicCoverageType.NotCovered, this.CoverageNotTouchedInfo);
            }

            if (!this.CoveragePartiallyTouchedInfo.Equals(lastCoverageColours.CoveragePartiallyTouchedInfo))
            {
                changes.Add(DynamicCoverageType.Partial, this.CoveragePartiallyTouchedInfo);
            }

            if (!this.DirtyInfo.Equals(lastCoverageColours.DirtyInfo))
            {
                changes.Add(DynamicCoverageType.Dirty, this.DirtyInfo);
            }

            if (!this.NewLineInfo.Equals(lastCoverageColours.NewLineInfo))
            {
                changes.Add(DynamicCoverageType.NewLine, this.NewLineInfo);
            }

            if (!this.NotIncludedInfo.Equals(lastCoverageColours.NotIncludedInfo))
            {
                changes.Add(DynamicCoverageType.NotIncluded, this.NotIncludedInfo);
            }

            return changes;
        }

        public IItemCoverageColours GetColour(DynamicCoverageType coverageType)
            => this.coverageTypeToFontAndColorsInfo[coverageType].ItemCoverageColours;
    }
}
