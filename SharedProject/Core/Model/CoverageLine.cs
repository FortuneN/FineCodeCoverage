using FineCodeCoverage.Engine.Cobertura;

namespace FineCodeCoverage.Engine.Model
{
	internal class CoverageLine
	{
		public Package Package { get; internal set; }
		public Class Class { get; internal set; }
		public Line Line { get; internal set; }
	}
}
