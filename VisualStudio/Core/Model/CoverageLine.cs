using FineCodeCoverage.Core.Cobertura;

namespace FineCodeCoverage.Core.Model
{
	public class CoverageLine
	{
		public Package Package { get; internal set; }
		public Class Class { get; internal set; }
		public Line Line { get; internal set; }
	}
}
