using FineCodeCoverage.Cobertura;
using FineCodeCoverage.Options;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Impl
{
	internal static class CoverageUtil
	{
		public const string CoverletName = "coverlet.console";
		public static string CoverletExePath { get; private set; }
		public static string AppDataCoverletFolder { get; private set; }
		public static Version CurrentCoverletVersion { get; private set; }
		public static Version MimimumCoverletVersion { get; } = Version.Parse("1.7.2");

		public const string ReportGeneratorName = "dotnet-reportgenerator-globaltool";
		public static string ReportGeneratorExePath { get; private set; }
		public static string AppDataReportGeneratorFolder { get; private set; }
		public static Version CurrentReportGeneratorVersion { get; private set; }
		public static Version MimimumReportGeneratorVersion { get; } = Version.Parse("4.6.7");

		public static string SummaryHtmlFilePath { get; private set; }
		public static string CoverageHtmlFilePath { get; private set; }
		public static string RiskHotspotsHtmlFilePath { get; private set; }
		public static List<CoverageLine> CoverageLines { get; private set; } = new List<CoverageLine>();

		public static string AppDataFolder { get; private set; }
		public static string[] ProjectExtensions { get; } = new string[] { ".csproj", ".vbproj" };
		public static ConcurrentDictionary<string, string> ProjectFoldersCache { get; } = new ConcurrentDictionary<string, string>();
		
		static CoverageUtil()
		{
			AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
			Directory.CreateDirectory(AppDataFolder);

			AppDataCoverletFolder = Path.Combine(AppDataFolder, "coverlet");
			Directory.CreateDirectory(AppDataCoverletFolder);
			GetCoverletVersion();

			AppDataReportGeneratorFolder = Path.Combine(AppDataFolder, "reportGenerator");
			Directory.CreateDirectory(AppDataReportGeneratorFolder);
			GetReportGeneratorVersion();

			// cleanup legacy folders
			Directory.GetDirectories(AppDataFolder, "*", SearchOption.TopDirectoryOnly).Where(x => x.Contains("__")).ToList().ForEach(x => Directory.Delete(x, true));
		}

		public static CoverageLine GetLine(string filePath, int lineNumber)
		{
			return CoverageLines
				.AsParallel()
				.SingleOrDefault(line =>
				{
					return line.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase)
						&& line.Line.Number == lineNumber;
				});
		}

		public static void Initialize()
		{
			if (CurrentCoverletVersion == null)
			{
				InstallCoverlet();
			}
			else if (CurrentCoverletVersion < MimimumCoverletVersion)
			{
				UpdateCoverlet();
			}

			if (CurrentReportGeneratorVersion == null)
			{
				InstallReportGenerator();
			}
			else if (CurrentReportGeneratorVersion < MimimumReportGeneratorVersion)
			{
				UpdateReportGenerator();
			}
		}

		private static string GetProjectFolderFromPath(string path)
		{
			if (ProjectFoldersCache.TryGetValue(path = path.ToLower(), out var result))
			{
				return result;
			}

			var parentFolder = Path.GetDirectoryName(path);

			if (parentFolder == null)
			{
				return null;
			}

			if (Directory.GetFiles(parentFolder).AsParallel().Any(x => ProjectExtensions.Any(y => x.EndsWith(y, StringComparison.OrdinalIgnoreCase))))
			{
				result = parentFolder;
			}
			else
			{
				result = GetProjectFolderFromPath(parentFolder);
			}

			return ProjectFoldersCache[path] = result;
		}

		private static Version GetCoverletVersion()
		{
			var title = "Coverlet -> Get Info";

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

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
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

		private static void UpdateCoverlet()
		{
			var title = "Coverlet -> Update";

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

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return;
			}

			GetCoverletVersion();

			Logger.Log(title, processOutput);
		}

		private static void InstallCoverlet()
		{
			var title = "Coverlet -> Install";

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

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return;
			}

			GetCoverletVersion();

			Logger.Log(title, processOutput);
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		private static bool RunCoverlet(AppSettings appSettings, string testDllFile, string coverageFolder, out string projectCoberturaFile, bool throwError = false)
		{
			var title = "Coverlet -> Run";

			var testDllFileInCoverageFolder = Path.Combine(coverageFolder, Path.GetFileName(testDllFile));

			var ouputFolder = Path.Combine(coverageFolder, "_outputfolder");
			if (Directory.Exists(ouputFolder)) Directory.Delete(ouputFolder, true);
			Directory.CreateDirectory(ouputFolder);

			projectCoberturaFile = Path.Combine(ouputFolder, "_project.cobertura.xml");

			if (File.Exists(projectCoberturaFile))
			{
				File.Delete(projectCoberturaFile);
			}

			var coverletSettings = new List<string>();

			coverletSettings.Add($@"""{testDllFileInCoverageFolder}""");

			coverletSettings.Add($@"--format ""cobertura""");

			foreach (var value in (appSettings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var value in (appSettings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--include ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var value in (appSettings.IncludeDirectory ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--include-directory ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var value in (appSettings.ExcludeByFile ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude-by-file ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
			}

			foreach (var value in (appSettings.ExcludeByAttribute ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				coverletSettings.Add($@"--exclude-by-attribute ""{value.Replace("\"", "\\\"").Trim(' ', '\'', '[', ']')}""");
			}

			if (appSettings.IncludeTestAssembly)
			{
				coverletSettings.Add("--include-test-assembly");
			}

			//https://github.com/coverlet-coverage/coverlet/issues/961
			//if (appSettings.SkipAutoProperties)
			//{
			//	coverletSettings.Add("--skipautoprops");
			//}

			//https://github.com/coverlet-coverage/coverlet/issues/962
			//foreach (var value in (appSettings.DoesNotReturnAttributes ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
			//{
			//	coverletSettings.Add($@"--does-not-return-attribute ""{value.Replace("\"", "\\\"")}""");
			//}

			coverletSettings.Add($@"--target ""dotnet""");

			coverletSettings.Add($@"--output ""{ projectCoberturaFile }""");

			coverletSettings.Add($"--targetargs \"test \"\"{testDllFileInCoverageFolder}\"\"\"");

			Logger.Log($"Arguments : {Environment.NewLine}{string.Join($"{Environment.NewLine}", coverletSettings)}");

			var processStartInfo = new ProcessStartInfo
			{
				FileName = CoverletExePath,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = string.Join(" ", coverletSettings),
			};
			
			var process = Process.Start(processStartInfo);

			if (!process.HasExited)
			{
				var stopWatch = new Stopwatch();
				stopWatch.Start();
				if (!Task.Run(() => process.WaitForExit()).Wait(TimeSpan.FromSeconds(appSettings.CoverletTimeout)))
				{
					stopWatch.Stop();
					Task.Run(() => { try { process.Kill(); } catch { } }).Wait(TimeSpan.FromSeconds(10));
					throw new Exception($"Coverlet timed out after {stopWatch.Elapsed.TotalSeconds} seconds (CoverletTimeout is {appSettings.CoverletTimeout} seconds)");
				}
			}

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				if (throwError)
				{
					throw new Exception(processOutput);
				}
				
				Logger.Log($"Error during {title}", processOutput);
				return false;
			}

			Logger.Log(title, processOutput);
			return true;
		}

		private static AppSettings GetSettings(string testDllFile)
		{
			// get global settings

			var settings = AppSettings.Get();

			// override with test project settings

			var testProjectFolder = GetProjectFolderFromPath(testDllFile);

			if (string.IsNullOrWhiteSpace(testProjectFolder))
			{
				return settings;
			}

			var testProjectFile = Directory.GetFiles(testProjectFolder, "*.*proj", SearchOption.TopDirectoryOnly).FirstOrDefault();

			if (string.IsNullOrWhiteSpace(testProjectFile) || !File.Exists(testProjectFile))
			{
				return settings;
			}

			XElement xproject;

			try
			{
				xproject = XElement.Parse(File.ReadAllText(testProjectFile));
			}
			catch (Exception ex)
			{
				Logger.Log("Failed to parse project file", ex);
				return settings;
			}

			var xsettings = xproject.Descendants("PropertyGroup").FirstOrDefault(x =>
			{
				var label = x.Attribute("Label")?.Value?.Trim() ?? string.Empty;

				if (!Vsix.Code.Equals(label, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				return true;
			});

			if (xsettings == null)
			{
				return settings;
			}

			foreach (var property in settings.GetType().GetProperties())
			{
				try
				{
					var xproperty = xsettings.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

					if (xproperty == null)
					{
						continue;
					}

					var strValue = xproperty.Value;

					if (string.IsNullOrWhiteSpace(strValue))
					{
						continue;
					}

					var strValueArr = strValue.Split('\n', '\r').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

					if (!strValue.Any())
					{
						continue;
					}

					if (TypeMatch(property.PropertyType, typeof(string)))
					{
						property.SetValue(settings, strValueArr.FirstOrDefault());
					}
					else if (TypeMatch(property.PropertyType, typeof(string[])))
					{
						property.SetValue(settings, strValueArr);
					}

					else if (TypeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
					{
						if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
					{
						var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(int), typeof(int?)))
					{
						if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
					{
						var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(short), typeof(short?)))
					{
						if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
						{
							property.SetValue(settings, vaue);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
					{
						var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(long), typeof(long?)))
					{
						if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
					{
						var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
					{
						if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
					{
						var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(double), typeof(double?)))
					{
						if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
					{
						var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(float), typeof(float?)))
					{
						if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
					{
						var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else if (TypeMatch(property.PropertyType, typeof(char), typeof(char?)))
					{
						if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
						{
							property.SetValue(settings, value);
						}
					}
					else if (TypeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
					{
						var arr = strValueArr.Where(x => char.TryParse(x, out var _)).Select(x => char.Parse(x));
						if (arr.Any()) property.SetValue(settings, arr);
					}

					else
					{
						throw new Exception($"Cannot handle '{property.PropertyType.Name}' yet");
					}
				}
				catch (Exception exception)
				{
					Logger.Log($"Failed to override '{property.Name}' setting", exception);
				}
			}

			// return

			return settings;
		}

		private static bool TypeMatch(Type type, params Type[] otherTypes)
		{
			return (otherTypes ?? new Type[0]).Any(ot => type == ot);
		}

		private static Version GetReportGeneratorVersion()
		{
			var title = "ReportGenerator -> Get Info";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataReportGeneratorFolder,
				Arguments = $"tool list --tool-path \"{AppDataReportGeneratorFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return null;
			}

			var outputLines = processOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var reportGeneratorLine = outputLines.FirstOrDefault(x => x.Trim().StartsWith(ReportGeneratorName, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(reportGeneratorLine))
			{
				// reportGenerator is not installed
				ReportGeneratorExePath = null;
				CurrentReportGeneratorVersion = null;
				return null;
			}

			var reportGeneratorLineTokens = reportGeneratorLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var reportGeneratorVersion = reportGeneratorLineTokens[1].Trim();

			CurrentReportGeneratorVersion = Version.Parse(reportGeneratorVersion);

			ReportGeneratorExePath = Directory.GetFiles(AppDataReportGeneratorFolder, "reportGenerator.exe", SearchOption.AllDirectories).FirstOrDefault()
						   ?? Directory.GetFiles(AppDataReportGeneratorFolder, "*reportGenerator*.exe", SearchOption.AllDirectories).FirstOrDefault();

			return CurrentReportGeneratorVersion;
		}

		private static void UpdateReportGenerator()
		{
			var title = "ReportGenerator -> Update";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataReportGeneratorFolder,
				Arguments = $"tool update {ReportGeneratorName} --verbosity normal --version {MimimumReportGeneratorVersion} --tool-path \"{AppDataReportGeneratorFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return;
			}

			GetReportGeneratorVersion();

			Logger.Log(title, processOutput);
		}

		private static void InstallReportGenerator()
		{
			var title = "ReportGenerator -> Install";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataReportGeneratorFolder,
				Arguments = $"tool install {ReportGeneratorName} --verbosity normal --version {MimimumReportGeneratorVersion} --tool-path \"{AppDataReportGeneratorFolder}\"",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return;
			}

			GetReportGeneratorVersion();

			Logger.Log(title, processOutput);
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		private static bool RunReportGenerator(IEnumerable<string> coberturaFiles, out string unifiedHtmlFile, out string unifiedXmlFile, bool throwError = false)
		{
			var title = "ReportGenerator -> Run";
			var ouputFolder = Path.GetDirectoryName(coberturaFiles.OrderBy(x => x).First()); // use location of first file to output reports
			
			Directory.GetFiles(ouputFolder, "*.htm*").ToList().ForEach(File.Delete); // delete html files if they exist

			unifiedHtmlFile = Path.Combine(ouputFolder, "index.html");
			unifiedXmlFile = Path.Combine(ouputFolder, "cobertura.xml");//??

			var processStartInfo = new ProcessStartInfo
			{
				FileName = ReportGeneratorExePath,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				//Arguments = $"\"-reports:{string.Join(";", coberturaFiles)}\" \"-targetdir:{ouputFolder}\" -reporttypes:Cobertura,HtmlInline_AzurePipelines_Dark",
				Arguments = $"\"-reports:{string.Join(";", coberturaFiles)}\" \"-targetdir:{ouputFolder}\" -reporttypes:Cobertura;HtmlInline_AzurePipelines",
			};

			var process = Process.Start(processStartInfo);

			if (!process.HasExited)
			{
				process.WaitForExit();
			}

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				if (throwError)
				{
					throw new Exception(processOutput);
				}

				Logger.Log($"Error during {title}", processOutput);
				return false;
			}

			Logger.Log(title, processOutput);
			return true;
		}

		private static string GetProcessOutput(Process process)
		{
			return string.Join(Environment.NewLine, new[] { process.StandardOutput?.ReadToEnd(), process.StandardError?.ReadToEnd() }.Where(x => !string.IsNullOrWhiteSpace(x)));
		}

		private static string Hash(string input)
		{
			using (var md5 = MD5.Create())
			{
				var inputBytes = Encoding.ASCII.GetBytes(input);
				var outputBytes = md5.ComputeHash(inputBytes);
				var outputSb = new StringBuilder();

				for (int i = 0; i < outputBytes.Length; i++)
				{
					outputSb.Append(outputBytes[i].ToString("X2"));
				}

				return outputSb.ToString();
			}
		}

		public static void ReloadCoverage(IEnumerable<string> testDllFiles, Action<Exception> marginHighlightsCallback, Action<Exception> outputWindowCallback, Action<Exception> doneCallback)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					// reset

					CoverageLines.Clear();
					SummaryHtmlFilePath = default;
					CoverageHtmlFilePath = default;
					RiskHotspotsHtmlFilePath = default;

					// process pipeline

					var projects = testDllFiles
					.AsParallel()
					.Select(testDllFile =>
					{
						var project = new CoverageProject();

						project.TestDllFileInOutputFolder = testDllFile;
						project.Settings = GetSettings(project.TestDllFileInOutputFolder);

						if (!project.Settings.Enabled)
						{
							project.FailureDescription = $"Disabled";
							return project;
						}
						
						project.ProjectFolder = GetProjectFolderFromPath(project.TestDllFileInOutputFolder);
						project.ProjectFile = Directory.GetFiles(project.ProjectFolder).FirstOrDefault(x => ProjectExtensions.Any(y => x.EndsWith(y, StringComparison.OrdinalIgnoreCase)));

						if (string.IsNullOrWhiteSpace(project.ProjectFile))
						{
							project.FailureDescription = $"Unsupported project type for DLL '{project.TestDllFileInOutputFolder}'";
							return project;
						}

						project.ProjectOutputFolder = Path.GetDirectoryName(project.TestDllFileInOutputFolder);
						project.WorkFolder = Path.Combine(AppDataFolder, Hash(project.ProjectFolder));
						project.TestDllFileInWorkFolder = Path.Combine(project.WorkFolder, Path.GetFileName(project.TestDllFileInOutputFolder));

						return project;
					})
					.Select(p => p.Step("Create Work Folder", project =>
					{
						// determine project properties

						Directory.CreateDirectory(project.WorkFolder);
					}))
					.Select(p => p.Step("Synchronize", project =>
					{
						// sync files from output folder to work folder where we do the analysis

						FileSynchronizationUtil.Synchronize(project.ProjectOutputFolder, project.WorkFolder);
					}))
					.Select(p => p.Step("Run Coverlet", project =>
					{
						// run coverlet process

						RunCoverlet(project.Settings, project.TestDllFileInWorkFolder, project.WorkFolder, out var projectCoberturaFile, true);

						project.ProjectCoberturaFile = projectCoberturaFile;
					}))
					.Where(x => !x.HasFailed)
					.ToArray();

					// project files

					var projectCoberturaFiles = projects
						.AsParallel()
						.Select(x => x.ProjectCoberturaFile)
						.ToArray();

					if (!projectCoberturaFiles.Any())
					{
						marginHighlightsCallback?.Invoke(default);
						outputWindowCallback?.Invoke(default);
						doneCallback?.Invoke(default);
						return;
					}

					// run reportGenerator process

					RunReportGenerator(projectCoberturaFiles, out var unifiedHtmlFile, out var unifiedXmlFile, true);

					// finalize

					Parallel.Invoke
					(
						() =>
						{
							try
							{
								ProcessCoberturaXmlFile(unifiedXmlFile, out var coverageLines);

								CoverageLines = coverageLines;

								marginHighlightsCallback?.Invoke(default);
							}
							catch (Exception exception)
							{
								marginHighlightsCallback?.Invoke(exception);
							}
						},
						() =>
						{
							try
							{
								ProcessCoberturaHtmlFile(unifiedHtmlFile, out var summaryHtmlFile, out var coverageHtmlFile, out var riskHotspotsHtmlFile);

								SummaryHtmlFilePath = summaryHtmlFile;
								CoverageHtmlFilePath = coverageHtmlFile;
								RiskHotspotsHtmlFilePath = riskHotspotsHtmlFile;

								outputWindowCallback?.Invoke(default);
							}
							catch (Exception exception)
							{
								outputWindowCallback?.Invoke(exception);
							}
						}
					);

					doneCallback?.Invoke(default);
				}
				catch (Exception exception)
				{
					doneCallback?.Invoke(exception);
				}
			});
		}

		private static void ProcessCoberturaHtmlFile(string htmlFile, out string summaryHtmlFile, out string coverageHtmlFile, out string riskHotspotsHtmlFile)
		{
			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				// read [htmlFile] into memory

				var htmlFileContent = File.ReadAllText(htmlFile);

				// delete all html files

				var folder = Path.GetDirectoryName(htmlFile);
				
				// create and save doc util

				HtmlDocument createHtmlDocument(HtmlSegment segment)
				{
					var doc = new HtmlDocument();

					doc.OptionFixNestedTags = true;
					doc.OptionAutoCloseOnEnd = true;
					
					doc.LoadHtml(htmlFileContent);

					doc.DocumentNode.QuerySelectorAll(".footer").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
					doc.DocumentNode.QuerySelectorAll(".container").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:0;border:0"));
					doc.DocumentNode.QuerySelectorAll(".containerleft").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:0;border:0"));
					doc.DocumentNode.QuerySelectorAll(".containerleft > h1 , .containerleft > p").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));

					switch (segment)
					{
						case HtmlSegment.Summary:
							doc.DocumentNode.QuerySelectorAll("risk-hotspots").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							doc.DocumentNode.QuerySelectorAll("coverage-info").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							break;

						case HtmlSegment.Coverage:
							doc.DocumentNode.QuerySelectorAll("risk-hotspots").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							doc.DocumentNode.QuerySelectorAll(".overview").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							break;

						case HtmlSegment.RiskHotspots:
							doc.DocumentNode.QuerySelectorAll(".overview").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							doc.DocumentNode.QuerySelectorAll("coverage-info").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
							break;
					}

					return doc;
				}

				string saveHtmlDocument(HtmlSegment segment, HtmlDocument doc)
				{
					var path = Path.Combine(folder, $"_{segment}.html".ToLower());

					// DOM changes

					switch (segment)
					{
						case HtmlSegment.Summary:
							var table = doc.DocumentNode.QuerySelectorAll("table.overview").First();
							var tableRows = table.QuerySelectorAll("tr").ToArray();
							try { tableRows[0].SetAttributeValue("style", "display:none"); } catch {}
							try { tableRows[1].SetAttributeValue("style", "display:none"); } catch { }
							try { tableRows[10].SetAttributeValue("style", "display:none"); } catch { }
							try { tableRows[10].SetAttributeValue("style", "display:none"); } catch { }
							try { tableRows[11].SetAttributeValue("style", "display:none"); } catch { }
							try { tableRows[12].SetAttributeValue("style", "display:none"); } catch { }
							break;

						case HtmlSegment.Coverage:
							break;

						case HtmlSegment.RiskHotspots:
							break;
					}

					var body = doc.DocumentNode.QuerySelector("body");
					body.SetAttributeValue("oncontextmenu", "return false;");
					
					// TEXT changes

					var html = doc.DocumentNode.OuterHtml;

					switch (segment)
					{
						case HtmlSegment.Summary:
							break;

						case HtmlSegment.Coverage:
							
							html = html.Replace("branchCoverageAvailable = true", "branchCoverageAvailable = false");

							html = string.Join(
								Environment.NewLine,
								html.Split('\r', '\n')
								.Select(line =>
								{
									if (line.IndexOf(@"""name"":") != -1 && line.IndexOf(@"""rp"":") != -1 && line.IndexOf(@"""cl"":") != -1)
									{
										var lineJO = JObject.Parse(line.TrimEnd(','));
										var name = lineJO.Value<string>("name");

										if (name.Equals("AutoGeneratedProgram"))
										{
											// output line

											line = string.Empty;
										}
										else
										{
											// simplify name
											
											var lastIndexOfDotInName = name.LastIndexOf('.');
											if (lastIndexOfDotInName != -1) lineJO["name"] = name.Substring(lastIndexOfDotInName).Trim('.');

											// prefix the url with #

											lineJO["rp"] = $"#{lineJO.Value<string>("rp")}";

											// output line

											line = $"{lineJO.ToString(Formatting.None)},";
										}
									}

									return line;
								})
							);
							
							break;

						case HtmlSegment.RiskHotspots:

							html = html.Replace("https://en.wikipedia.org/wiki/Cyclomatic_complexity", "#");
							
							html = string.Join(
								Environment.NewLine,
								html.Split('\r', '\n')
								.Select(line =>
								{
									if (line.IndexOf(@"""assembly"":") != -1 && line.IndexOf(@"""class"":") != -1 && line.IndexOf(@"""reportPath"":") != -1)
									{
										//"assembly": "PayAtService.BusinessLogic", "class": "DayEndReconciliationFileParser", "reportPath": "PayAtService.BusinessLogic_DayEndReconciliationFileParser.html", "methodName": "ParseAsync(System.IO.Stream)", "methodShortName": "ParseAsync(...)", "fileIndex": 0, "line": 38,

										var lineJO = JObject.Parse($"{{ {line.TrimEnd(',')} }}");

										// simplify class

										var _class = lineJO.Value<string>("class");
										var lastIndexOfDotInClass = _class.LastIndexOf('.');
										if (lastIndexOfDotInClass != -1) lineJO["class"] = _class.Substring(lastIndexOfDotInClass).Trim('.');

										// prefix the urls with #

										lineJO["reportPath"] = $"#{lineJO.Value<string>("reportPath")}";

										// output line

										line = $"{lineJO.ToString(Formatting.None).Trim('{', '}')},";
									}

									return line;
								})
							);
							
							break;
					}

					html = html.Replace("</head>", $@"
						<style type=""text/css""> 
							table td {{ text-overflow:  ellipsis | clip; white-space: nowrap; }}
							a, a:hover {{ color: #0078D4; text-decoration: none; cursor: pointer; }}
							table th, table td {{ font-size: small; white-space: nowrap; word-break: normal; }}
							body {{ -webkit-user-select:none;-moz-user-select:none;-ms-user-select:none;-o-user-select:none;user-select:none }}
						</style>
						</head>
					");

					html = html.Replace("</body>", $@"
						<script type=""text/javascript"">
							
							var htmlExtension = '.html';
							var pageFolder = '{folder.Trim('\\').Replace("\\", "\\\\")}\\';
							
							var eventListener = function (element, event, func) {{
								if (element.addEventListener) element.addEventListener(event, func, false);
								else if (element.attachEvent) element.attachEvent('on' + event, func);
								else element['on' + event] = func;
							}};
							
							eventListener(document, 'click', function (event) {{
								
								var target = event.target;
								if (target.tagName.toLowerCase() !== 'a') return;
								
								var href = target.getAttribute('href');
								if (!href || href[0] !== '#') return;
								
								var htmlExtensionIndex = href.toLowerCase().indexOf(htmlExtension);
								if (htmlExtensionIndex == -1) return;
								
								if (event.preventDefault) event.preventDefault()
								if (event.stopPropagation) event.stopPropagation();
								
								var fullHref = pageFolder + href.substring(1, htmlExtensionIndex + htmlExtension.length);
								var fileLine = href.substring(htmlExtensionIndex + htmlExtension.length);
								
								if (fileLine.indexOf('#') != -1) fileLine = fileLine.substring(fileLine.indexOf('#') + 1).replace('file', '').replace('line', '').split('_');
								else fileLine = ['0', '0'];
								
								window.external.OpenFile(fullHref, parseInt(fileLine[0]), parseInt(fileLine[1]));
								
								return false;
							}});
							
						</script>
						</body>
					");

					// save

					File.WriteAllText(path, html);
					return path;
				}

				// produce segment html files

				summaryHtmlFile = saveHtmlDocument(HtmlSegment.Summary, createHtmlDocument(HtmlSegment.Summary));
				coverageHtmlFile = saveHtmlDocument(HtmlSegment.Coverage, createHtmlDocument(HtmlSegment.Coverage));
				riskHotspotsHtmlFile = saveHtmlDocument(HtmlSegment.RiskHotspots, createHtmlDocument(HtmlSegment.RiskHotspots));
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		private static void ProcessCoberturaXmlFile(string unifiedXmlFile, out List<CoverageLine> coverageLines)
		{
			coverageLines = new List<CoverageLine>();

			var report = CoberturaReportLoader.LoadReportFile(unifiedXmlFile);

			foreach (var package in report.Packages.Package)
			{
				foreach (var classs in package.Classes.Class)
				{
					foreach (var line in classs.Lines.Line)
					{
						coverageLines.Add(new CoverageLine
						{
							Package = package,
							Class = classs,
							Line = line
						});
					}
				}
			}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var assemblyName = new AssemblyName(args.Name);

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

				// try resolve by name

				try
				{
					var assembly = Assembly.Load(assemblyName.Name);
					if (assembly != null) return assembly;
				}
				catch
				{
					// ignore
				}

				// try resolve by path

				try
				{
					var dllName = $"{assemblyName.Name}.dll";
					var projectDllPath = Path.GetDirectoryName(typeof(CoverageUtil).Assembly.Location);
					var dllPath = Directory.GetFiles(projectDllPath, "*.dll", SearchOption.AllDirectories).FirstOrDefault(x => Path.GetFileName(x).Equals(x.Equals(dllName, StringComparison.OrdinalIgnoreCase)));

					if (!string.IsNullOrWhiteSpace(dllPath))
					{
						var assembly = Assembly.LoadFile(dllPath);
						if (assembly != null) return assembly;
					}
				}
				catch
				{
					// ignore
				}
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			}

			return null;
		}

		private enum HtmlSegment
		{
			Summary,
			Coverage,
			RiskHotspots
		}
	}
}