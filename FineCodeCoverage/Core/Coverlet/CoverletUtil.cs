using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.Utilities;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletUtil {
		void Initialize(string appDataFolder);
		Task<bool> RunCoverletAsync(CoverageProject project, bool throwError = false);
	}

	[Export(typeof(ICoverletUtil))]
    internal class CoverletUtil:ICoverletUtil
	{
		private const string CoverletName = "coverlet.console";
        private readonly IProcessUtil processUtil;
        private readonly ILogger logger;
        private string coverletExePath;
		private string appDataCoverletFolder;
		private Version currentCoverletVersion;
		private Version MimimumCoverletVersion { get; } = Version.Parse("1.7.2");

		[ImportingConstructor]
		public CoverletUtil(IProcessUtil processUtil, ILogger logger)
        {
            this.processUtil = processUtil;
            this.logger = logger;
        }
		public void Initialize(string appDataFolder)
		{
			appDataCoverletFolder = Path.Combine(appDataFolder, "coverlet");
			Directory.CreateDirectory(appDataCoverletFolder);
			GetCoverletVersion();

			if (currentCoverletVersion == null)
			{
				InstallCoverlet();
			}
			else if (currentCoverletVersion < MimimumCoverletVersion)
			{
				UpdateCoverlet();
			}
		}

		private Version GetCoverletVersion()
		{
			var title = "Coverlet Get Info";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = appDataCoverletFolder,
				Arguments = $"tool list --tool-path \"{appDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				logger.Log($"{title} Error", processOutput);
				return null;
			}

			var outputLines = processOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletLine = outputLines.FirstOrDefault(x => x.Trim().StartsWith(CoverletName, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(coverletLine))
			{
				// coverlet is not installed
				coverletExePath = null;
				currentCoverletVersion = null;
				return null;
			}

			var coverletLineTokens = coverletLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletVersion = coverletLineTokens[1].Trim();

			currentCoverletVersion = Version.Parse(coverletVersion);

			coverletExePath = Directory.GetFiles(appDataCoverletFolder, "coverlet.exe", SearchOption.AllDirectories).FirstOrDefault()
						   ?? Directory.GetFiles(appDataCoverletFolder, "*coverlet*.exe", SearchOption.AllDirectories).FirstOrDefault();

			return currentCoverletVersion;
		}

		private void UpdateCoverlet()
		{
			var title = "Coverlet Update";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = appDataCoverletFolder,
				Arguments = $"tool update {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{appDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				logger.Log($"{title} Error", processOutput);
				return;
			}

			GetCoverletVersion();

			logger.Log(title, processOutput);
		}

		private void InstallCoverlet()
		{
			var title = "Coverlet Install";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = appDataCoverletFolder,
				Arguments = $"tool install {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{appDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				logger.Log($"{title} Error", processOutput);
				return;
			}

			GetCoverletVersion();

			logger.Log(title, processOutput);
		}

		public async Task<bool> RunCoverletAsync(CoverageProject project, bool throwError = false)
		{
			var title = $"Coverlet Run ({project.ProjectName})";

			var coverletSettings = new List<string>();

			coverletSettings.Add($@"""{project.TestDllFile}""");

			coverletSettings.Add($@"--format ""cobertura""");

			foreach (var value in (project.Settings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var referencedProjectExcludedFromCodeCoverage in project.ReferencedProjects.Where(x => x.ExcludeFromCodeCoverage))
			{
				coverletSettings.Add($@"--exclude ""[{referencedProjectExcludedFromCodeCoverage.AssemblyName}]*""");
			}

			foreach (var value in (project.Settings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--include ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
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

			logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", coverletSettings)}");

			var result = await processUtil
			.ExecuteAsync(new ExecuteRequest
			{
				FilePath = coverletExePath,
				Arguments = string.Join(" ", coverletSettings),
				WorkingDirectory = project.ProjectOutputFolder
			});
			

			if(result != null)
            {
				/*
				0 - Success.
				1 - If any test fails.
				2 - Coverage percentage is below threshold.
				3 - Test fails and also coverage percentage is below threshold.
			*/
				if (result.ExitCode > 3)
				{
					if (throwError)
					{
						throw new Exception(result.Output);
					}

					logger.Log($"{title} Error", result.Output);
					return false;
				}

				logger.Log(title, result.Output);

				return true;
			}
			return false;
		}
	}
}
