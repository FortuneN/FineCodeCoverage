using FineCodeCoverage.Core.Model;
using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Coverlet
{
	public interface ICoverletService
	{
		public void Initialize();

		public Version GetCoverletVersion();

		public void UpdateCoverlet();

		public void InstallCoverlet();

		public Task RunCoverletAsync(CoverageProject project);
	}
}
