using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class CoverageColours : ICoverageColours
    {
        public IItemCoverageColours CoverageTouchedColours { get; }
        public IItemCoverageColours CoverageNotTouchedColours { get; }
        public IItemCoverageColours CoveragePartiallyTouchedColours { get; }
        public CoverageColours(
            IItemCoverageColours coverageTouchedColors,
            IItemCoverageColours coverageNotTouched,
            IItemCoverageColours coveragePartiallyTouchedColors
        )
        {
            CoverageTouchedColours = coverageTouchedColors;
            CoverageNotTouchedColours = coverageNotTouched;
            CoveragePartiallyTouchedColours = coveragePartiallyTouchedColors;
        }

        internal Dictionary<CoverageType, IItemCoverageColours> GetChanges(CoverageColours lastCoverageColours)
        {
            var changes = new Dictionary<CoverageType, IItemCoverageColours>();
            if (lastCoverageColours == null) return new Dictionary<CoverageType, IItemCoverageColours>
            {
                { CoverageType.Covered, CoverageTouchedColours},
                {CoverageType.NotCovered, CoverageNotTouchedColours },
                { CoverageType.Partial, CoveragePartiallyTouchedColours}
            };

            if (!CoverageTouchedColours.Equals(lastCoverageColours.CoverageTouchedColours))
            {
                changes.Add(CoverageType.Covered, CoverageTouchedColours);
            }
            if (!CoverageNotTouchedColours.Equals(lastCoverageColours.CoverageNotTouchedColours))
            {
                changes.Add(CoverageType.NotCovered, CoverageNotTouchedColours);
            }
            if (!CoveragePartiallyTouchedColours.Equals(lastCoverageColours.CoveragePartiallyTouchedColours))
            {
                changes.Add(CoverageType.Partial, CoveragePartiallyTouchedColours);
            }
            return changes;
        }

        public IItemCoverageColours GetColor(CoverageType coverageType)
        {
            switch (coverageType)
            {
                case CoverageType.Partial:
                    return CoveragePartiallyTouchedColours;
                case CoverageType.NotCovered:
                    return CoverageNotTouchedColours;
                case CoverageType.Covered:
                    return CoverageTouchedColours;
            }
            return default;
        }

    }

}
