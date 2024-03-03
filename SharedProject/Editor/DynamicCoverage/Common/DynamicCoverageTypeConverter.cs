using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal static class DynamicCoverageTypeConverter
    {
        public static DynamicCoverageType Convert(CoverageType coverageType)
        {
            DynamicCoverageType dynamicCoverageType = DynamicCoverageType.Covered;
            switch (coverageType)
            {
                case CoverageType.NotCovered:
                    dynamicCoverageType = DynamicCoverageType.NotCovered;
                    break;
                case CoverageType.Partial:
                    dynamicCoverageType = DynamicCoverageType.Partial;
                    break;
            }

            return dynamicCoverageType;
        }

        public static CoverageType Convert(DynamicCoverageType coverageType)
        {
            CoverageType converted = CoverageType.Covered;
            switch (coverageType)
            {
                case DynamicCoverageType.NotCovered:
                    converted = CoverageType.NotCovered;
                    break;
                case DynamicCoverageType.Partial:
                    converted = CoverageType.Partial;
                    break;
            }

            return converted;
        }
    }
}
