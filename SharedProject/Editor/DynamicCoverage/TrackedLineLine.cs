using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal static class  DynamicCoverageTypeConverter
    {
        public static DynamicCoverageType Convert(CoverageType coverageType)
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
    }

    internal class TrackedLineLine : IDynamicLine
    {        
        private readonly CoverageType lineCoverageType;
        
        public TrackedLineLine(ILine line)
        {
            Number = line.Number;
            lineCoverageType = line.CoverageType;
            CoverageType = DynamicCoverageTypeConverter.Convert(lineCoverageType);
        }

        public int Number { get; set; }
        public DynamicCoverageType CoverageType { get; private set; }
    }
}
