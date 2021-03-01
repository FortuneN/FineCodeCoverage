using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletConsoleDotnetToolsGlobalExecutor : ICoverletConsoleExecutor
    {

    }

    [Export(typeof(ICoverletConsoleDotnetToolsGlobalExecutor))]
    internal class CoverletConsoleDotnetToolsGlobalExecutor : ICoverletConsoleDotnetToolsGlobalExecutor
    {
        private readonly IDotNetToolListCoverlet dotNetToolListCoverlet;

        [ImportingConstructor]
		public CoverletConsoleDotnetToolsGlobalExecutor(IDotNetToolListCoverlet dotNetToolListCoverlet)
        {
            this.dotNetToolListCoverlet = dotNetToolListCoverlet;
        }
		public ExecuteRequest GetRequest(ICoverageProject coverageProject, string coverletSettings)
        {
            if (coverageProject.Settings.CoverletConsoleGlobal)
            {
				var details = dotNetToolListCoverlet.Global();
				if(details == null)
                {
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
