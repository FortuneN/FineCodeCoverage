using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using Newtonsoft.Json;

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

		public static string AppDataFolder { get; private set; }
		public static string[] ProjectExtensions { get; } = new string[] { ".csproj", ".vbproj" };
		public static List<CoverageProject> CoverageProjects { get; } = new List<CoverageProject>();
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

		private static bool RunCoverlet(string testDllFile, string coverageFolder, out string jsonFile, out string coberturaFile)
		{
			var title = "Coverlet -> Run";

			var testDllFileInCoverageFolder = Path.Combine(coverageFolder, Path.GetFileName(testDllFile));

			var ouputFolder = Path.Combine(coverageFolder, "_outputfolder");
			if (Directory.Exists(ouputFolder)) Directory.Delete(ouputFolder, true);
			Directory.CreateDirectory(ouputFolder);

			var ouputFilePrefix = Path.Combine(ouputFolder, "_outputfile");

			jsonFile = $"{ouputFilePrefix}.json";

			coberturaFile = $"{ouputFilePrefix}.cobertura.xml";

			new[] { jsonFile, coberturaFile }.Where(File.Exists).ToList().ForEach(File.Delete); // delete files if they exist

			var processStartInfo = new ProcessStartInfo
			{
				FileName = CoverletExePath,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = $"\"{testDllFileInCoverageFolder}\" --include-test-assembly --format json --format cobertura --target dotnet --output \"{ouputFilePrefix}\" --targetargs \"test \"\"{testDllFileInCoverageFolder}\"\"\"",
			};
			
			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
				Logger.Log($"Error during {title}", processOutput);
				return false;
			}

			Logger.Log(title, processOutput);
			return true;
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

		private static bool RunReportGenerator(string coberturaFile, out string htmlFile)
		{
			var title = "ReportGenerator -> Run";

			var ouputFolder = Path.GetDirectoryName(coberturaFile);
			
			Directory.GetFiles(ouputFolder, "*.htm*").ToList().ForEach(File.Delete); // delete html files if they exist

			htmlFile = Path.Combine(ouputFolder, "index.html");

			var processStartInfo = new ProcessStartInfo
			{
				FileName = ReportGeneratorExePath,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				//Arguments = $"\"-reports:{coberturaFile}\" \"-targetdir:{ouputFolder}\" -reporttypes:HtmlInline_AzurePipelines_Dark",
				Arguments = $"\"-reports:{coberturaFile}\" \"-targetdir:{ouputFolder}\" -reporttypes:HtmlInline_AzurePipelines",
			};

			var process = Process.Start(processStartInfo);

			process.WaitForExit();

			var processOutput = GetProcessOutput(process);

			if (process.ExitCode != 0)
			{
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

					// sync built files to coverage folder

					var buildFolder = Path.GetDirectoryName(testDllFile);

					FileSynchronizationUtil.Synchronize(buildFolder, coverageFolder);

					// run coverlet process

					if (RunCoverlet(testDllFile, coverageFolder, out var coverageJsonFile, out var coberturaFile))
					{
						ProcessJsonFile(coverageJsonFile);
						ProcessCoberturaFile(coberturaFile);
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

		private static void ProcessJsonFile(string jsonFile)
		{
			var coverageFileContent = File.ReadAllText(jsonFile);
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
							var branchesJArray = (JArray)methodJObject["Branches"];

							foreach (var lineProperty in linesJObject.Properties())
							{
								var lineNumber = int.Parse(lineProperty.Name);
								var lineHitCount = lineProperty.Value.Value<int>();
								var lineBranches = branchesJArray.Select(b => b as JObject).Where(b => b.Value<int>("Line") == lineNumber).Select(b => b.ToObject<CoverageLineBranch>()).ToList();

								sourceFile.Lines.Add(new CoverageLine
								{
									ClassName = className,
									MethodName = methodName,
									LineNumber = lineNumber,
									HitCount = lineHitCount,
									LineBranches = lineBranches
								});
							}
						}
					}
				}
			}
		}

		private static void ProcessCoberturaFile(string coberturaFile)
		{
			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				// clear

				SummaryHtmlFilePath = string.Empty;
				CoverageHtmlFilePath = string.Empty;
				RiskHotspotsHtmlFilePath = string.Empty;

				// run report generator

				if (!RunReportGenerator(coberturaFile, out var htmlFile))
				{
					return;
				}

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

				void saveHtmlDocument(HtmlSegment segment, HtmlDocument doc)
				{
					var path = Path.Combine(folder, $"_{segment}.html".ToLower());

					// DOM changes

					switch (segment)
					{
						case HtmlSegment.Summary:
							SummaryHtmlFilePath = path;
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
							CoverageHtmlFilePath = path;

							break;

						case HtmlSegment.RiskHotspots:
							RiskHotspotsHtmlFilePath = path;
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
				}

				// produce segment html files

				saveHtmlDocument(HtmlSegment.Summary, createHtmlDocument(HtmlSegment.Summary));
				saveHtmlDocument(HtmlSegment.Coverage, createHtmlDocument(HtmlSegment.Coverage));
				saveHtmlDocument(HtmlSegment.RiskHotspots, createHtmlDocument(HtmlSegment.RiskHotspots));
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
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