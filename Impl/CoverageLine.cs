namespace FineCodeCoverage.Impl
{
	internal class CoverageLine
	{
		public string ClassName { get; set; }
		public string MethodName { get; set; }
		public int LineNumber { get; set; }
		public int HitCount { get; set; }
	}
}
