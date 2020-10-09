using FineCodeCoverage.Engine.Utilities;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine.ReportGenerator
{
	internal partial class ReportGeneratorUtil
	{
		public const string ReportGeneratorName = "dotnet-reportgenerator-globaltool";
		public static string ReportGeneratorExePath { get; private set; }
		public static string AppDataReportGeneratorFolder { get; private set; }
		public static Version CurrentReportGeneratorVersion { get; private set; }
		public static Version MimimumReportGeneratorVersion { get; } = Version.Parse("4.6.7");

		public static void Initialize(string appDataFolder)
		{
			AppDataReportGeneratorFolder = Path.Combine(appDataFolder, "reportGenerator");
			Directory.CreateDirectory(AppDataReportGeneratorFolder);
			GetReportGeneratorVersion();

			if (CurrentReportGeneratorVersion == null)
			{
				InstallReportGenerator();
			}
			else if (CurrentReportGeneratorVersion < MimimumReportGeneratorVersion)
			{
				UpdateReportGenerator();
			}
		}

		public static Version GetReportGeneratorVersion()
		{
			var title = "ReportGenerator Get Info";

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

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
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

			ReportGeneratorExePath = Directory.GetFiles(AppDataReportGeneratorFolder, "reportGenerator.exe"  , SearchOption.AllDirectories).FirstOrDefault()
						          ?? Directory.GetFiles(AppDataReportGeneratorFolder, "*reportGenerator*.exe", SearchOption.AllDirectories).FirstOrDefault();

			return CurrentReportGeneratorVersion;
		}

		public static void UpdateReportGenerator()
		{
			var title = "ReportGenerator Update";

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

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
				return;
			}

			GetReportGeneratorVersion();

			Logger.Log(title, processOutput);
		}

		public static void InstallReportGenerator()
		{
			var title = "ReportGenerator Install";

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

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				Logger.Log($"{title} Error", processOutput);
				return;
			}

			GetReportGeneratorVersion();

			Logger.Log(title, processOutput);
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		public static bool RunReportGenerator(IEnumerable<string> coverOutputFiles, out string unifiedHtmlFile, out string unifiedXmlFile, bool throwError = false)
		{
			var title = "ReportGenerator Run";
			var ouputFolder = Path.GetDirectoryName(coverOutputFiles.OrderBy(x => x).First()); // use location of first file to output reports

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
				//Arguments = $"\"-reports:{string.Join(";", coverOutputFiles)}\" \"-targetdir:{ouputFolder}\" -reporttypes:Cobertura,HtmlInline_AzurePipelines_Dark",
				Arguments = $"\"-reports:{string.Join(";", coverOutputFiles)}\" \"-targetdir:{ouputFolder}\" -reporttypes:Cobertura;HtmlInline_AzurePipelines",
			};

			var process = Process.Start(processStartInfo);

			if (!process.HasExited)
			{
				process.WaitForExit();
			}

			var processOutput = process.GetOutput();

			if (process.ExitCode != 0)
			{
				if (throwError)
				{
					throw new Exception(processOutput);
				}

				Logger.Log($"{title} Error", processOutput);
				return false;
			}

			Logger.Log(title, processOutput);
			return true;
		}

		public static void ProcessCoberturaHtmlFile(string htmlFile, out string summaryHtmlFile, out string coverageHtmlFile, out string riskHotspotsHtmlFile)
		{
			var result = AssemblyUtil.RunInAssemblyResolvingContext(() =>
			{
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
							try { tableRows[0].SetAttributeValue("style", "display:none"); } catch { }
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

					html = html.Replace(".table-fixed", ".table-fixed-ignore-me");

					html = string.Join(
						Environment.NewLine,
						html.Split('\r', '\n')
						.Select(line =>
						{
							if (line.StartsWith(".column"))
							{
								line = $"{line.Substring(0, line.IndexOf('{')).Trim('{')} {{white-space: nowrap; width:1%;}}";
							}

							return line;
						}));

					html = html.Replace("</head>", $@"
						<style type=""text/css"">
						    table td {{ white-space: nowrap; }}
							table.coverage {{ width:150px;height:13px }}
							a, a:hover {{ color: #0078D4; text-decoration: none; cursor: pointer; }}
							body {{ -webkit-user-select:none;-moz-user-select:none;-ms-user-select:none;-o-user-select:none;user-select:none }}
							table.overview th, table.overview td {{ font-size: small; white-space: nowrap; word-break: normal; padding-left:10px;padding-right:10px; }}
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

							html = html.Replace("</head>", $@"
								<style type=""text/css"">
									table.overview.table-fixed.stripped > thead > tr > th:nth-of-type(4) > a:nth-of-type(2) {{ display:none; }}
								</style>
								</head>
							");

							break;
					}

					// save

					File.WriteAllText(path, html);
					return path;
				}

				// produce segment html files

				var _summaryHtmlFile = saveHtmlDocument(HtmlSegment.Summary, createHtmlDocument(HtmlSegment.Summary));
				var _coverageHtmlFile = saveHtmlDocument(HtmlSegment.Coverage, createHtmlDocument(HtmlSegment.Coverage));
				var _riskHotspotsHtmlFile = saveHtmlDocument(HtmlSegment.RiskHotspots, createHtmlDocument(HtmlSegment.RiskHotspots));

				// return

				return (_summaryHtmlFile, _coverageHtmlFile, _riskHotspotsHtmlFile);
			});

			summaryHtmlFile = result._summaryHtmlFile;
			coverageHtmlFile = result._coverageHtmlFile;
			riskHotspotsHtmlFile = result._riskHotspotsHtmlFile;
		}
	}
}
