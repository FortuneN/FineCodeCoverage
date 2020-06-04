using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FineCodeCoverage.Impl
{
    internal static class CoverageUtil
	{
		private static readonly string TempFolder;

		private static readonly string SyncDllPath;

		private static readonly string CoverletDllPath;

		private static readonly string[] ProjectExtensions = new string[] { ".csproj", ".vbproj" };

		private static readonly List<CoverageProject> CoverageProjects = new List<CoverageProject>();

		private static readonly ConcurrentDictionary<string, string> ProjectFoldersCache = new ConcurrentDictionary<string, string>();

		static CoverageUtil()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var codeBase = assembly.CodeBase;
			var codeBaseUri = new UriBuilder(codeBase);
			var codeBasePath = Uri.UnescapeDataString(codeBaseUri.Path);
			var codeBaseFolderPath = Path.GetDirectoryName(codeBasePath);
			var itemTemplatesFolder = Path.Combine(codeBaseFolderPath, "ItemTemplates");

			SyncDllPath = Path.Combine(itemTemplatesFolder, "synctool", "synctool.dll");
			CoverletDllPath = Path.Combine(itemTemplatesFolder, "coverlet.console", "coverlet.console.dll");
			
			TempFolder = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name);
			Directory.CreateDirectory(TempFolder);
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

		public static void LoadCoverageFromTestDllFile(string testDllFile, Action<Exception> callback = null)
		{
			if (string.IsNullOrWhiteSpace(testDllFile) || !File.Exists(testDllFile))
			{
				return;
			}

			ThreadPool.QueueUserWorkItem(x =>
			{
				try
				{
					// project folder

					var testProjectFolder = GetProjectFolderFromPath(testDllFile);

					if (string.IsNullOrWhiteSpace(testProjectFolder))
					{
						return;
					}

					// coverage folder

					var coverageFolder = Path.Combine(TempFolder, testProjectFolder.Replace('-', '_').Replace('.', '_').Replace(':', '_').Replace('\\', '_').Replace('/', '_'));

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

					// other

					var buildFolder = Path.GetDirectoryName(testDllFile);
					var testDllFileInCoverageFolder = Path.Combine(coverageFolder, Path.GetFileName(testDllFile));

					// sync process

					var syncProcess = Process.Start(new ProcessStartInfo
					{
						FileName = "dotnet",
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						Arguments = $"\"{SyncDllPath}\" sync \"{buildFolder}\" \"{coverageFolder}\" --silent",
					});

					syncProcess.WaitForExit();

					if (syncProcess.ExitCode != 0)
					{
						throw new Exception(syncProcess.StandardOutput.ReadToEnd());
					}

					// coverage process

					var coverageProcess = Process.Start(new ProcessStartInfo
					{
						FileName = "dotnet",
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						Arguments = $"\"{CoverletDllPath}\" \"{testDllFileInCoverageFolder}\" --include-test-assembly --format json --target dotnet --output \"{coverageFile}\" --targetargs \"test \"\"{testDllFileInCoverageFolder}\"\" --no-build\"",
					});

					coverageProcess.WaitForExit();

					if (coverageProcess.ExitCode != 0)
					{
						throw new Exception(coverageProcess.StandardOutput.ReadToEnd());
					}

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