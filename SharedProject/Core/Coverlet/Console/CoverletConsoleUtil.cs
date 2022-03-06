using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(ICoverletConsoleUtil))]
	internal class CoverletConsoleUtil : ICoverletConsoleUtil
	{
		private readonly IProcessUtil processUtil;
		private readonly ILogger logger;
        private readonly IFCCCoverletConsoleExecutor fccExecutor;
		private readonly List<ICoverletConsoleExecutor> executors;

		[ImportingConstructor]
		public CoverletConsoleUtil(
			IProcessUtil processUtil, 
			ILogger logger,
			[Import(typeof(ICoverletConsoleDotnetToolsGlobalExecutor))]
			ICoverletConsoleExecutor globalExecutor,
			[Import(typeof(ICoverletConsoleCustomPathExecutor))]
			ICoverletConsoleExecutor customPathExecutor,
			[Import(typeof(ICoverletConsoleDotnetToolsLocalExecutor))]
			ICoverletConsoleExecutor localExecutor,
			IFCCCoverletConsoleExecutor fccExecutor
			)
		{
			this.processUtil = processUtil;
			this.logger = logger;

            executors = new List<ICoverletConsoleExecutor>
            {
                localExecutor,
                customPathExecutor,
                globalExecutor,
                fccExecutor
            };

            this.fccExecutor = fccExecutor;
        }
		public void Initialize(string appDataFolder, CancellationToken cancellationToken)
		{
			fccExecutor.Initialize(appDataFolder, cancellationToken);
		}

		// for now FCCCoverletConsoleExeProvider can return null for exe path

		internal ExecuteRequest GetExecuteRequest(ICoverageProject project, string coverletSettings)
        {
			foreach(var exeProvider in executors)
            {
				var executeRequest = exeProvider.GetRequest(project, coverletSettings);
				if(executeRequest != null)
                {
					return executeRequest;
                }
            }
			return null;//todo change to throw when using zip file
        }

		internal List<string> GetCoverletSettings(ICoverageProject project)
        {
			var coverletSettings = new List<string>();

			coverletSettings.Add($@"""{project.TestDllFile}""");

			coverletSettings.Add($@"--format ""cobertura""");

			foreach (var value in (project.Settings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var referencedProjectExcludedFromCodeCoverage in project.ExcludedReferencedProjects)
			{
				coverletSettings.Add($@"--exclude ""[{referencedProjectExcludedFromCodeCoverage}]*""");
			}

			foreach (var value in (project.Settings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--include ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var includedReferencedProject in project.IncludedReferencedProjects)
			{
				coverletSettings.Add($@"--include ""[{includedReferencedProject}]*""");
			}

			foreach (var value in (project.Settings.ExcludeByFile ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude-by-file ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var value in (project.Settings.ExcludeByAttribute ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude-by-attribute ""{value.Replace("\"", "\\\"").Trim(' ', '\'', '[', ']')}""");
			}

			if (project.Settings.IncludeTestAssembly)
			{
				coverletSettings.Add("--include-test-assembly");
			}

			coverletSettings.Add($@"--target ""dotnet""");

			coverletSettings.Add($@"--threshold-type line");

			coverletSettings.Add($@"--threshold-stat total");

			coverletSettings.Add($@"--threshold 0");

			coverletSettings.Add($@"--output ""{ project.CoverageOutputFile }""");

			var runSettings = !string.IsNullOrWhiteSpace(project.RunSettingsFile) ? $@"--settings """"{project.RunSettingsFile}""""" : default;
			coverletSettings.Add($@"--targetargs ""test  """"{project.TestDllFile}"""" --nologo --blame {runSettings} --results-directory """"{project.CoverageOutputFolder}"""" --diag """"{project.CoverageOutputFolder}/diagnostics.log""""  """);

			return coverletSettings;
		}

		public async Task RunAsync(ICoverageProject project, CancellationToken cancellationToken)
		{
			var title = $"Coverlet Run ({project.ProjectName})";

			var coverletSettings = GetCoverletSettings(project);

			logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", coverletSettings)}");

			var result = await processUtil
			.ExecuteAsync(GetExecuteRequest(project, string.Join(" ", coverletSettings)), cancellationToken);


			
				/*
				0 - Success.
				1 - If any test fails.
				2 - Coverage percentage is below threshold.
				3 - Test fails and also coverage percentage is below threshold.
			*/
				if (result.ExitCode > 3)
				{
					logger.Log($"{title} Error. Exit code: {result.ExitCode}");
					logger.Log($"{title} Error. Output: ", result.Output);
					
					throw new Exception(result.Output);
				}

				logger.Log(title, result.Output);
		}
	}
}
