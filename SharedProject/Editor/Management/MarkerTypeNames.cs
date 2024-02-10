using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export]
    public class MarkerTypeNames
    {
        public string Covered { get; } = "Coverage Touched Area";
        public string NotCovered { get; } = "Coverage Not Touched Area";
        public string PartiallyCovered { get; } = "Coverage Partially Touched Area";
    }
}
