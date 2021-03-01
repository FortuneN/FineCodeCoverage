using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    public interface ICoverletConsoleExeFinder
    {
        string FindInFolder(string folder, SearchOption searchOption);

    }
    public class CoverletConsoleExeFinder
    {
        public string FindInFolder(string folder, SearchOption searchOption)
        {
            return Directory.GetFiles(folder, "coverlet.exe", searchOption).FirstOrDefault()
                           ?? Directory.GetFiles(folder, "*coverlet*.exe", searchOption).FirstOrDefault();
        }
    }

    [Export(typeof(IFCCCoverletConsoleExecutor))]
    internal class FCCCoverletConsoleExeProvider : IFCCCoverletConsoleExecutor
    {
		[ImportingConstructor]
		public FCCCoverletConsoleExeProvider(ILogger logger)
        {
            this.logger = logger;
        }

        private const string CoverletName = "coverlet.console";
		private readonly ILogger logger;
        private string coverletExePath;
		private string appDataCoverletFolder;
		private Version currentCoverletVersion;
		private Version MimimumCoverletVersion { get; } = Version.Parse("1.7.2");
		public ExecuteRequest GetRequest(ICoverageProject coverageProject, string coverletSettings)
        {
			return new ExecuteRequest
			{
				FilePath = coverletExePath,
				Arguments = coverletSettings,
				WorkingDirectory = coverageProject.ProjectOutputFolder
			};

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



	}
}
