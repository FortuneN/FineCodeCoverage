using FineCodeCoverage.Cobertura;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLine
	{
		public Package Package { get; internal set; }
		public Class Class { get; internal set; }
		public Line Line { get; internal set; }
	}
}
