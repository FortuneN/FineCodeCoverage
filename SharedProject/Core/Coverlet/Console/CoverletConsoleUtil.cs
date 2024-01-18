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
	internal interface ICoverletExeArgumentsProvider
	{
		   List<string> GetArguments(ICoverageProject project);
	}

	[Export(typeof(ICoverletExeArgumentsProvider))]
    internal class CoverletExeArgumentsProvider : ICoverletExeArgumentsProvider
    {
        private IEnumerable<string> SanitizeExcludesOrIncludes(string[] excludesOrIncludes)
        {
            return (excludesOrIncludes ?? new string[0])
                .Where(x => x != null)
                .Select(x => x.Trim(' ', '\'', '\"'))
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public List<string> GetArguments(ICoverageProject project)
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

            foreach (var value in SanitizeExcludesOrIncludes(project.Settings.ExcludeByAttribute).Select(EnsureAttributeTypeUnqualified))
            {
                var withoutAttributeBrackets = value.Trim('[', ']');
                coverletSettings.Add($@"--exclude-by-attribute {value}");
            }

            if (project.Settings.IncludeTestAssembly)
            {
                coverletSettings.Add("--include-test-assembly");
            }

            coverletSettings.Add($@"--target ""dotnet""");

            coverletSettings.Add($@"--threshold-type line");

            coverletSettings.Add($@"--threshold-stat total");

            coverletSettings.Add($@"--threshold 0");

            coverletSettings.Add($@"--output ""{project.CoverageOutputFile}""");

            var runSettings = !string.IsNullOrWhiteSpace(project.RunSettingsFile) ? $@"--settings """"{project.RunSettingsFile}""""" : default;
            coverletSettings.Add($@"--targetargs ""test  """"{project.TestDllFile}"""" --nologo --blame {runSettings} --results-directory """"{project.CoverageOutputFolder}"""" --diag """"{project.CoverageOutputFolder}/diagnostics.log""""  """);

            return coverletSettings;
        }

        private string EnsureAttributeTypeUnqualified(string attributeType) => attributeType.Split('.').Last();

    }

    internal interface ICoverletConsoleExecuteRequestProvider
    {
        ExecuteRequest GetExecuteRequest(ICoverageProject project, string coverletSettings);
    }

    [Export(typeof(ICoverletConsoleExecuteRequestProvider))]
    internal class CoverletConsoleExecuteRequestProvider : ICoverletConsoleExecuteRequestProvider
    {
        private List<ICoverletConsoleExecutor> executors;

        [ImportingConstructor]
        public CoverletConsoleExecuteRequestProvider(
            [Import(typeof(ICoverletConsoleDotnetToolsGlobalExecutor))]
            ICoverletConsoleExecutor globalExecutor,
            [Import(typeof(ICoverletConsoleCustomPathExecutor))]
            ICoverletConsoleExecutor customPathExecutor,
            [Import(typeof(ICoverletConsoleDotnetToolsLocalExecutor))]
            ICoverletConsoleExecutor localExecutor,
            IFCCCoverletConsoleExecutor fccExecutor
        )
        {
            executors = new List<ICoverletConsoleExecutor>
            {
                localExecutor,
                customPathExecutor,
                globalExecutor,
                fccExecutor
            };
        }
        // for now FCCCoverletConsoleExeProvider can return null for exe path
        public ExecuteRequest GetExecuteRequest(ICoverageProject project, string coverletSettings)
        {
            foreach (var exeProvider in executors)
            {
                var executeRequest = exeProvider.GetRequest(project, coverletSettings);
                if (executeRequest != null)
                {
                    return executeRequest;
                }
            }
            return null;//todo change to throw when using zip file
        }
    }

    [Export(typeof(ICoverletConsoleUtil))]
	internal class CoverletConsoleUtil : ICoverletConsoleUtil
	{
		private readonly IProcessUtil processUtil;
		private readonly ILogger logger;
        private readonly ICoverletConsoleExecuteRequestProvider coverletConsoleExecuteRequestProvider;
        private readonly IFCCCoverletConsoleExecutor fccExecutor;
        private readonly ICoverletExeArgumentsProvider coverletExeArgumentsProvider;
        private readonly List<ICoverletConsoleExecutor> executors;

		[ImportingConstructor]
		public CoverletConsoleUtil(
			IProcessUtil processUtil, 
			ILogger logger,
            ICoverletConsoleExecuteRequestProvider coverletConsoleExecuteRequestProvider,
            IFCCCoverletConsoleExecutor fccExecutor,
            ICoverletExeArgumentsProvider coverletExeArgumentsProvider
            )
		{
			this.processUtil = processUtil;
			this.logger = logger;
            this.coverletConsoleExecuteRequestProvider = coverletConsoleExecuteRequestProvider;
            this.fccExecutor = fccExecutor;
            this.coverletExeArgumentsProvider = coverletExeArgumentsProvider;
        }
		public void Initialize(string appDataFolder, CancellationToken cancellationToken)
		{
			fccExecutor.Initialize(appDataFolder, cancellationToken);
		}

		public async Task RunAsync(ICoverageProject project, CancellationToken cancellationToken)
		{
			var title = $"Coverlet Run ({project.ProjectName})";

			var coverletSettings = coverletExeArgumentsProvider.GetArguments(project);

            var executingLogLines = new List<string> { $"{title} - Arguments" };
            executingLogLines.AddRange(coverletSettings);
            logger.Log(executingLogLines);

			var result = await processUtil.ExecuteAsync(
                coverletConsoleExecuteRequestProvider.GetExecuteRequest(project, string.Join(" ", coverletSettings)), 
                cancellationToken
            );

			/*
			0 - Success.
			1 - If any test fails.
			2 - Coverage percentage is below threshold.
			3 - Test fails and also coverage percentage is below threshold.
			*/
			if (result.ExitCode > 3)
			{
				logger.Log($"{title} Error. Exit code: {result.ExitCode}", result.Output);
					
				throw new Exception(result.Output);
			}

			logger.Log(title, result.Output);
		}
	}
}
