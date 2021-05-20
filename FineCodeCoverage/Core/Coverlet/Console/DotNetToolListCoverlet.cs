using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IDotNetToolListCoverlet))]
    internal class DotNetToolListCoverlet : IDotNetToolListCoverlet
    {
		private const string CoverletPackageId = "coverlet.console";
		private readonly ILogger logger;
        private readonly IDotNetToolListExecutor executor;
        private readonly IDotNetToolListParser parser;

        [ImportingConstructor]
		public DotNetToolListCoverlet(ILogger logger, IDotNetToolListExecutor executor, IDotNetToolListParser parser)
        {
            this.logger = logger;
            this.executor = executor;
            this.parser = parser;
        }

		private CoverletToolDetails ExecuteAndParse(Func<IDotNetToolListExecutor,DotNetToolListExecutionResult> execution )
        {
			var result = execution(executor);
			if(result.ExitCode != 0)
            {
				var title = $"Dotnet tool list Coverlet";
				logger.Log($"{title} Error", result.Output);
				return null;
			}
			List<DotNetTool> tools = null;
            try
            {
				tools = parser.Parse(result.Output);
			}
			catch (Exception)
            {
				var title = $"Dotnet tool list Coverlet";
				logger.Log($"{title} Error parsing", result.Output);
				return null;
			}
			
			var coverletConsoleTool = tools.FirstOrDefault(tool => tool.PackageId == CoverletPackageId);
			if(coverletConsoleTool == null)
            {
				return null;
            }

			return new CoverletToolDetails
			{
				Version = coverletConsoleTool.Version,
				Command = coverletConsoleTool.Commands
			};
		}

        public CoverletToolDetails Global()
        {
			return ExecuteAndParse(executor => executor.Global());
		}
		
		public CoverletToolDetails Local(string directory)
        {
			return ExecuteAndParse(executor => executor.Local(directory));
        }

        public CoverletToolDetails GlobalToolsPath(string directory)
        {
			return ExecuteAndParse(executor => executor.GlobalToolsPath(directory));
		}

	}
}
