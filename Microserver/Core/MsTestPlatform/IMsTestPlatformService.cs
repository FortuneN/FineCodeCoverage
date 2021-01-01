using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform
{
	public interface IMsTestPlatformService
	{
		public Task InitializeAsync();

		public string GetMsTestPlatformExePath();

		public Task<Version> GetMsTestPlatformVersionAsync();

		public Task UpdateMsTestPlatformAsync();

		public Task InstallMsTestPlatformAsync();
	}
}
