using CliWrap;
using CliWrap.Buffered;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine.Coverlet
{
	internal class CoverletUtil
	{
		public const string CoverletName = "coverlet.console";
		public static string CoverletExePath { get; private set; }
		public static string AppDataCoverletFolder { get; private set; }
		public static Version CurrentCoverletVersion { get; private set; }
		public static Version MimimumCoverletVersion { get; } = Version.Parse("1.7.2");

		public static void Initialize(string appDataFolder)
		{
			AppDataCoverletFolder = Path.Combine(appDataFolder, "coverlet");
			Directory.CreateDirectory(AppDataCoverletFolder);
			GetCoverletVersion();

			if (CurrentCoverletVersion == null)
			{
				InstallCoverlet();
			}
			else if (CurrentCoverletVersion < MimimumCoverletVersion)
			{
				UpdateCoverlet();
			}
		}

		public static Version GetCoverletVersion()
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
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool list --tool-path \"{AppDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
				return null;
			}

			var outputLines = processOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletLine = outputLines.FirstOrDefault(x => x.Trim().StartsWith(CoverletName, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(coverletLine))
			{
				// coverlet is not installed
				CoverletExePath = null;
				CurrentCoverletVersion = null;
				return null;
			}

			var coverletLineTokens = coverletLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletVersion = coverletLineTokens[1].Trim();

			CurrentCoverletVersion = Version.Parse(coverletVersion);

			CoverletExePath = Directory.GetFiles(AppDataCoverletFolder, "coverlet.exe", SearchOption.AllDirectories).FirstOrDefault()
						   ?? Directory.GetFiles(AppDataCoverletFolder, "*coverlet*.exe", SearchOption.AllDirectories).FirstOrDefault();

			return CurrentCoverletVersion;
		}

		public static void UpdateCoverlet()
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
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool update {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{AppDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
				return;
			}

			GetCoverletVersion();

			Logger.Log(title, processOutput);
		}

		public static void InstallCoverlet()
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
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool install {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{AppDataCoverletFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
				return;
			}

			GetCoverletVersion();

			Logger.Log(title, processOutput);
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		public static bool RunCoverlet(CoverageProject project, bool throwError = false)
		{
			var title = $"Coverlet Run ({project.ProjectName})";

			if (File.Exists(project.CoverToolOutputFile))
			{
				File.Delete(project.CoverToolOutputFile);
			}

			if (Directory.Exists(project.WorkOutputFolder))
			{
				Directory.Delete(project.WorkOutputFolder, true);
			}

			Directory.CreateDirectory(project.WorkOutputFolder);

			var coverletSettings = new List<string>();

			coverletSettings.Add($@"""{project.TestDllFileInWorkFolder}""");

			coverletSettings.Add($@"--format ""cobertura""");

			foreach (var value in (project.Settings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
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

			coverletSettings.Add($@"--output ""{ project.CoverToolOutputFile }""");

			coverletSettings.Add($@"--targetargs ""test  """"{project.TestDllFileInWorkFolder}"""" --nologo --blame --results-directory """"{project.WorkOutputFolder}"""" --diag """"{project.WorkOutputFolder}/diagnostics.log""""  """);

			Logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", coverletSettings)}");
			
			var result = ProcessUtil
			.ExecuteAsync(new ExecuteRequest
			{
				FilePath = CoverletExePath,
				Arguments = string.Join(" ", coverletSettings),
				WorkingDirectory = project.WorkFolder
			})
			.GetAwaiter()
			.GetResult();

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

				Logger.Log($"{title} Error", result.Output);
				return false;
			}

			Logger.Log(title, result.Output);
			return true;
		}

		//public static (Process Process, string OutputData, string ErrorData) CMD(string fileName, string arguments, string workingDirectory = null)
		//{
		//	var process = new Process
		//	{
		//		StartInfo = new ProcessStartInfo
		//		{
		//			FileName = fileName,
		//			Arguments = arguments,
		//			CreateNoWindow = true,
		//			UseShellExecute = false,
		//			RedirectStandardError = true,
		//			RedirectStandardOutput = true,
		//			WorkingDirectory = workingDirectory,
		//			WindowStyle = ProcessWindowStyle.Hidden,
		//		}
		//	};

		//	var outputData = new StringBuilder();
		//	var errorData = new StringBuilder();

		//	process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
		//	{
		//		if (e?.Data == null)
		//		{
		//			return;
		//		}

		//		outputData.AppendLine(e?.Data ?? string.Empty);
		//	};

		//	process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
		//	{
		//		if (e?.Data == null)
		//		{
		//			return;
		//		}

		//		errorData?.AppendLine(e?.Data ?? string.Empty);
		//	};

		//	process.Start();
		//	process.BeginOutputReadLine();
		//	process.BeginErrorReadLine();
		//	process.WaitForExit();

		//	return (process, outputData.ToString(), errorData.ToString());
		//}
	}
}
