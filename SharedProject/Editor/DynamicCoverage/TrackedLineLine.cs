using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal static class  DirtyCoverageTypeMapper
    {
        public static DynamicCoverageType GetClean(CoverageType coverageType)
        {
            var dynamicCoverageType = DynamicCoverageType.Covered;
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

        public static CoverageType GetClean(DynamicCoverageType dynamicCoverageType)
        {
            var coverageType = CoverageType.Covered;
            switch (dynamicCoverageType)
            {
                case DynamicCoverageType.NotCovered:
                    coverageType = CoverageType.NotCovered;
                    break;
                case DynamicCoverageType.Partial:
                    coverageType = CoverageType.Partial;
                    break;
               // case DynamicCoverageType.NewLine:
                  //  throw new System.Exception("Invalid DynamicCoverageType");
            }
            return coverageType;
        }
    }
    class TrackedLineLine : IDynamicLine
    {        
        private readonly CoverageType lineCoverageType;
        public TrackedLineLine(ILine line)
        {
            Number = line.Number;
            lineCoverageType = line.CoverageType;
            CoverageType = DirtyCoverageTypeMapper.GetClean(lineCoverageType);
        }

        public int Number { get; set; }
        public DynamicCoverageType CoverageType { get; private set; }
    }
}
