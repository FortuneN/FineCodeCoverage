using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
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
