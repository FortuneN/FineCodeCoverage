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
		private static readonly string TempFolder;

        private static readonly string DashLine = "----------------------------------------------------------------------------------------";

		private static readonly string[] ProjectExtensions = new string[] { ".csproj", ".vbproj" };

		private static readonly List<CoverageProject> CoverageProjects = new List<CoverageProject>();

		private static readonly ConcurrentDictionary<string, string> ProjectFoldersCache = new ConcurrentDictionary<string, string>();

		static CoverageUtil()
		{
			TempFolder = Path.Combine(Path.GetTempPath(), ProjectMetaData.Id);
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

        private static bool CoverletIsInstalled()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                UseShellExecute = false,
                Arguments = "tool list -g",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd() + '|' + process.StandardError.ReadToEnd();

            return output.ToLower().Contains("coverlet");
        }

        private static void InstallOrUpdateCoverlet()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "tool install --global coverlet.console",
            });

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return;
            }

            var standardOutput = process.StandardOutput.ReadToEnd();
                
            if (standardOutput.ToLower().Contains("already installed"))
            {
                return;
            }

            var standardError = process.StandardError.ReadToEnd();
                
            if (standardError.ToLower().Contains("already installed"))
            {
                return;
            }

            if (CoverletIsInstalled())
            {
                return;
            }

            var output = string.IsNullOrWhiteSpace(standardOutput) ? standardError : standardOutput;

            throw new Exception($"{DashLine}{Environment.NewLine}FAILED WHILE INSTALLING COVERLET{Environment.NewLine}{DashLine}{Environment.NewLine}{output}");
        }

        private static void RunCoverlet(string testDllFile, string coverageFolder, string coverageFile)
        {
            InstallOrUpdateCoverlet();

            var testDllFileInCoverageFolder = Path.Combine(coverageFolder, Path.GetFileName(testDllFile));

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "coverlet",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"\"{testDllFileInCoverageFolder}\" --include-test-assembly --format json --target dotnet --output \"{coverageFile}\" --targetargs \"test \"\"{testDllFileInCoverageFolder}\"\" --no-build\"",
            });

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return;
            }

            var output = process.StandardOutput.ReadToEnd();
            
            if (string.IsNullOrWhiteSpace(output))
            {
                output = process.StandardError.ReadToEnd();
            }

            throw new Exception($"{DashLine}{Environment.NewLine}FAILED WHILE RUNNING COVERLET{Environment.NewLine}{DashLine}{Environment.NewLine}{output}");
        }

        private static string PathToName(string path)
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

                    var coverageFolder = Path.Combine(TempFolder, PathToName(testProjectFolder));

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