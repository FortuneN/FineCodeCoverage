using System;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Model;

namespace FineCodeCoverage.Core.OpenCover
{
	public interface IOpenCoverService
	{
		public Task InitializeAsync();

		public Task<Version> GetVersionAsync();

		public Task<Version> UpdateVersionAsync();

		public Task<Version> InstallAsync();

		public Task RunOpenCoverAsync(CoverageProject project);
	}
}
