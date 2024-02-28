using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedLineLine : IDynamicLine
    {        
        private readonly CoverageType lineCoverageType;
        
        public TrackedLineLine(ILine line)
        {
            Number = line.Number - 1;
            lineCoverageType = line.CoverageType;
            CoverageType = DynamicCoverageTypeConverter.Convert(lineCoverageType);
        }

        public int Number { get; set; }
        public DynamicCoverageType CoverageType { get; private set; }
    }
}
