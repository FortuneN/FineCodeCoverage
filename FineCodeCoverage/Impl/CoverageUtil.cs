using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FineCodeCoverage.Impl
{
	internal static class CoverageUtil
	{
		public const string CoverletName = "coverlet.console";

		public static string CoverletExePath { get; private set; }

		public static string AppDataFolder { get; private set; }

		public static string AppDataCoverletFolder { get; private set; }

		public static Version CurrentCoverletVersion { get; private set; }

		public static Version MimimumCoverletVersion { get; } = Version.Parse("1.7.0");

		public static string[] ProjectExtensions { get; } = new string[] { ".csproj", ".vbproj" };

		public static List<CoverageProject> CoverageProjects { get; } = new List<CoverageProject>();

		public static ConcurrentDictionary<string, string> ProjectFoldersCache { get; } = new ConcurrentDictionary<string, string>();

		public static string DashLine { get; } = $"{Environment.NewLine}----------------------------------------------------------------------------------------{Environment.NewLine}";

		static CoverageUtil()
		{
			AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
			Directory.CreateDirectory(AppDataFolder);

			AppDataCoverletFolder = Path.Combine(AppDataFolder, "coverlet");
			Directory.CreateDirectory(AppDataCoverletFolder);

			GetCoverletInfo();
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

			if (Directory.GetFiles(parentFolder).Any(x => ProjectExtensions.Any(y => x.EndsWith(y, StringComparison.OrdinalIgnoreCase))))
			{
				result = parentFolder;
			}
			else
			{
				result = GetProjectFolderFromPath(parentFolder);
			}

			return ProjectFoldersCache[path] = result;
		}

		public static CoverageLine GetCoverageLine(string sourceFilePath, int lineNumber)
		{
			var projectFolder = GetProjectFolderFromPath(sourceFilePath);
			
			if (string.IsNullOrWhiteSpace(projectFolder))
			{
				return null;
			}

			var coverageProject = CoverageProjects.FirstOrDefault(cp => cp.FolderPath.Equals(projectFolder, StringComparison.OrdinalIgnoreCase));
			
			if (coverageProject == null)
			{
				return null;
			}

			var coverageSourceFile = coverageProject.SourceFiles.FirstOrDefault(sf => sf.FilePath.Equals(sourceFilePath, StringComparison.OrdinalIgnoreCase));

			if (coverageSourceFile == null)
			{
				return null;
			}

			return coverageSourceFile.Lines.FirstOrDefault(l => l.LineNumber == lineNumber);
		}

		private static void GetCoverletInfo()
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool list --tool-path \"{AppDataCoverletFolder}\"",
			});

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				ThrowException(process);
			}

			var outputLines = process.StandardOutput.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletLine = outputLines.FirstOrDefault(x => x.Trim().StartsWith(CoverletName, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(coverletLine))
			{
				// coverlet is not installed
				CoverletExePath = null;
				CurrentCoverletVersion = null;
				return;
			}

			var coverletLineTokens = coverletLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var coverletVersion = coverletLineTokens[1].Trim();

			CurrentCoverletVersion = Version.Parse(coverletVersion);

			CoverletExePath = Directory.GetFiles(AppDataCoverletFolder, "coverlet.exe", SearchOption.AllDirectories).FirstOrDefault()
						   ?? Directory.GetFiles(AppDataCoverletFolder, "*coverlet*.exe", SearchOption.AllDirectories).FirstOrDefault();
		}

		public static void UpdateCoverlet()
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool update {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{AppDataCoverletFolder}\"",
			});

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				ThrowException(process);
			}

			GetCoverletInfo();
		}

		public static void InstallCoverlet()
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "dotnet",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppDataCoverletFolder,
				Arguments = $"tool install {CoverletName} --verbosity normal --version {MimimumCoverletVersion} --tool-path \"{AppDataCoverletFolder}\"",
			});

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				ThrowException(process);
			}

			GetCoverletInfo();
		}

		private static void RunCoverlet(string testDllFile, string coverageFolder, string coverageFile)
		{
			var testDllFileInCoverageFolder = Path.Combine(coverageFolder, Path.GetFileName(testDllFile));

			var process = Process.Start(new ProcessStartInfo
			{
				FileName = CoverletExePath,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = $"\"{testDllFileInCoverageFolder}\" --include-test-assembly --format json --target dotnet --output \"{coverageFile}\" --targetargs \"test \"\"{testDllFileInCoverageFolder}\"\" --no-build\"",
			});

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				ThrowException(process);
			}
		}

		private static void ThrowException(Process process)
		{
			throw new Exception($"{DashLine}{process.StandardError?.ReadToEnd()}");
		}

		private static string ConvertPathToName(string path)
		{
			path = path.Replace(' ', '_').Replace('-', '_').Replace('.', '_').Replace(':', '_').Replace('\\', '_').Replace('/', '_');

			foreach (var character in Path.GetInvalidPathChars())
			{
				path = path.Replace(character, '_');
			}

			return path;
		}

		public static void LoadCoverageFromTestDllFile(string testDllFile, Action<Exception> callback = null)
		{
			ThreadPool.QueueUserWorkItem(x =>
			{
				try
				{
					// project folder

					var testProjectFolder = GetProjectFolderFromPath(testDllFile);

					if (string.IsNullOrWhiteSpace(testProjectFolder))
					{
						throw new Exception("Could not establish project folder");
					}

					// coverage folder

					var coverageFolder = Path.Combine(AppDataFolder, ConvertPathToName(testProjectFolder));

					if (!Directory.Exists(coverageFolder))
					{
						Directory.CreateDirectory(coverageFolder);
					}

					// coverage file

					var coverageFile = Path.Combine(coverageFolder, "coverage.json");

					if (File.Exists(coverageFile))
					{
						File.Delete(coverageFile);
					}

					// sync built files to coverage folder

					var buildFolder = Path.GetDirectoryName(testDllFile);
					FileSynchronizationUtil.Synchronize(buildFolder, coverageFolder);

					// coverage process

					RunCoverlet(testDllFile, coverageFolder, coverageFile);

					// reload

					var coverageFileContent = File.ReadAllText(coverageFile);
					var coverageFileJObject = JObject.Parse(coverageFileContent);

					foreach (var coverageDllFileProperty in coverageFileJObject.Properties())
					{
						var coverageDllFileJObject = (JObject)coverageDllFileProperty.Value;

						foreach (var sourceFileProperty in coverageDllFileJObject.Properties())
						{
							var sourceFilePath = sourceFileProperty.Name;
							var projectFolderPath = GetProjectFolderFromPath(sourceFilePath);

							if (string.IsNullOrWhiteSpace(projectFolderPath))
							{
								continue;
							}

							var sourceFileProject = CoverageProjects.SingleOrDefault(pr => pr.FolderPath.Equals(projectFolderPath, StringComparison.OrdinalIgnoreCase));

							if (sourceFileProject == null)
							{
								sourceFileProject = new CoverageProject
								{
									FolderPath = projectFolderPath
								};

								CoverageProjects.Add(sourceFileProject);
							}

							var sourceFile = sourceFileProject.SourceFiles.SingleOrDefault(sf => sf.FilePath.Equals(sourceFilePath, StringComparison.OrdinalIgnoreCase));

							if (sourceFile == null)
							{
								sourceFile = new CoverageSourceFile
								{
									FilePath = sourceFilePath
								};

								sourceFileProject.SourceFiles.Add(sourceFile);
							}

							sourceFile.Lines.Clear();
							var sourceFileJObject = (JObject)sourceFileProperty.Value;

							foreach (var classProperty in sourceFileJObject.Properties())
							{
								var className = classProperty.Name;
								var classJObject = (JObject)classProperty.Value;

								foreach (var methodProperty in classJObject.Properties())
								{
									var methodName = methodProperty.Name;
									var methodJObject = (JObject)methodProperty.Value;
									var linesJObject = (JObject)methodJObject["Lines"];

									foreach (var lineProperty in linesJObject.Properties())
									{
										var lineNumber = int.Parse(lineProperty.Name);
										var lineHitCount = lineProperty.Value.Value<int>();

										sourceFile.Lines.Add(new CoverageLine
										{
											ClassName = className,
											MethodName = methodName,
											LineNumber = lineNumber,
											HitCount = lineHitCount
										});
									}
								}
							}
						}
					}

					// callback

					callback?.Invoke(null);
				}
				catch (Exception ex)
				{
					callback?.Invoke(ex);
				}
			});
		}
	}
}