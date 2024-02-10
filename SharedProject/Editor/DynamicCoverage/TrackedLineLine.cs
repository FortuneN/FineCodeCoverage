using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class TrackedLineLine : IDynamicLine
    {
        public TrackedLineLine(ILine line)
        {
            Number = line.Number;
            CoverageType = line.CoverageType;
        }

        public int Number { get; set; }

        public CoverageType CoverageType { get; }

        public bool IsDirty { get; set; }
    }
}
