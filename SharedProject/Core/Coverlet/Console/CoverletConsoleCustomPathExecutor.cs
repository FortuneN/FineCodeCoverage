using System.ComponentModel.Composition;
using System.IO;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletConsoleCustomPathExecutor : ICoverletConsoleExecutor { }

    [Export(typeof(ICoverletConsoleCustomPathExecutor))]
    internal class CoverletConsoleCustomPathExecutor : ICoverletConsoleCustomPathExecutor
    {
        public ExecuteRequest GetRequest(ICoverageProject coverageProject,string coverletSettings)
        {
            var coverletConsoleCustomPath = coverageProject.Settings.CoverletConsoleCustomPath;
            if (string.IsNullOrWhiteSpace(coverletConsoleCustomPath))
            {
				return null;
            }
            if (File.Exists(coverletConsoleCustomPath) && Path.GetExtension(coverletConsoleCustomPath) == ".exe")
            {
                return new ExecuteRequest
                {
                    FilePath = coverletConsoleCustomPath,
                    Arguments = coverletSettings,
                    WorkingDirectory = coverageProject.ProjectOutputFolder
                };
            }
			return null;
        }
    }
}
