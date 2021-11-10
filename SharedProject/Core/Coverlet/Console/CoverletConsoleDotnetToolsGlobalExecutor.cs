using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletConsoleDotnetToolsGlobalExecutor : ICoverletConsoleExecutor
    {

    }

    [Export(typeof(ICoverletConsoleDotnetToolsGlobalExecutor))]
    internal class CoverletConsoleDotnetToolsGlobalExecutor : ICoverletConsoleDotnetToolsGlobalExecutor
    {
        private readonly IDotNetToolListCoverlet dotNetToolListCoverlet;
        private readonly ILogger logger;

        [ImportingConstructor]
		public CoverletConsoleDotnetToolsGlobalExecutor(IDotNetToolListCoverlet dotNetToolListCoverlet, ILogger logger)
        {
            this.dotNetToolListCoverlet = dotNetToolListCoverlet;
            this.logger = logger;
        }
		public ExecuteRequest GetRequest(ICoverageProject coverageProject, string coverletSettings)
        {
            if (coverageProject.Settings.CoverletConsoleGlobal)
            {
				var details = dotNetToolListCoverlet.Global();
				if(details == null)
                {
                    logger.Log("Unable to use Coverlet console global tool");
					return null;
                }
				return new ExecuteRequest
				{
                    FilePath = details.Command,
                    Arguments = coverletSettings,
                    WorkingDirectory = coverageProject.ProjectOutputFolder
				};
			}
			return null;
			
        }
    }
}
