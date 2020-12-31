using FineCodeCoverage.Core.Cobertura;

namespace FineCodeCoverage.Core.Model
{
	public class CoverageLine
	{
		public Package Package { get; set; }
		public Class Class { get; set; }
		public Line Line { get; set; }
	}
}
