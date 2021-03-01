using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletConsoleDotnetToolsLocalExecutor : ICoverletConsoleExecutor { }

    [Export(typeof(ICoverletConsoleDotnetToolsLocalExecutor))]
    internal class CoverletConsoleDotnetToolsLocalExecutor : ICoverletConsoleDotnetToolsLocalExecutor
    {
        private readonly IDotNetToolListCoverlet dotnetToolListCoverlet;
        private readonly IDotNetConfigFinder dotNetConfigFinder;

        [ImportingConstructor]
        public CoverletConsoleDotnetToolsLocalExecutor(IDotNetToolListCoverlet dotnetToolListCoverlet, IDotNetConfigFinder dotNetConfigFinder)
        {
            this.dotnetToolListCoverlet = dotnetToolListCoverlet;
            this.dotNetConfigFinder = dotNetConfigFinder;
        }
        public ExecuteRequest GetRequest(ICoverageProject coverageProject, string coverletSettings)
        {
            if (coverageProject.Settings.CoverletConsoleLocal)
            {
				foreach(var configContainingDirectory in dotNetConfigFinder.GetConfigDirectories(coverageProject.ProjectOutputFolder))
                {
                    var coverletToolDetails = dotnetToolListCoverlet.Local(configContainingDirectory);
                    if(coverletToolDetails != null)
                    {
                        return new ExecuteRequest
                        {
                            FilePath = "dotnet",
                            Arguments = coverletToolDetails.Command + " " + coverletSettings,
                            WorkingDirectory = configContainingDirectory
                        };
                    }
                }

                return null;
            }
			return null;
        }
    }
}
