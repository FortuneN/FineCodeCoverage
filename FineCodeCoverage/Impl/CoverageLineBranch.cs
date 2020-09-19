namespace FineCodeCoverage.Impl
{
	internal class CoverageLineBranch
	{
		public int Line { get; set; }
		public int Offset { get; set; }
		public int Path { get; set; }
		public int Ordinal { get; set; }
		public int Hits { get; set; }
	}
}
