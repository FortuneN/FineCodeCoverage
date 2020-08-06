using Xunit;
using FineCodeCoverage.Impl;

namespace FineCodeCoverage.UnitTests
{
	public class CoverageUtilTests
	{
		[Fact]
		public void Read()
		{
			_ = CoverageUtil.CurrentCoverletVersion;
		}

		[Fact]
		public void Install()
		{
			CoverageUtil.InstallCoverlet();
		}

		[Fact]
		public void Update()
		{
			CoverageUtil.UpdateCoverlet();
		}
	}
}
