using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedLineLine : IDynamicLine
    {        
        public TrackedLineLine(ILine line)
        {
            this.Number = line.Number - 1;
            this.CoverageType = DynamicCoverageTypeConverter.Convert(line.CoverageType);
        }

        public int Number { get; set; }
        public DynamicCoverageType CoverageType { get; private set; }
    }
}
