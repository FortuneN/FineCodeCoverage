using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.Utilities;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportGeneratorPlugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.ReportGenerator
{
	public class ReportGeneratorService : IReportGeneratorService
	{
		private const string ReportGeneratorName = "dotnet-reportgenerator-globaltool";
		private string ReportGeneratorExePath { get; set; }
		private string AppDataReportGeneratorFolder { get; set; }
		private Version CurrentReportGeneratorVersion { get; set; }
		private Version MimimumReportGeneratorVersion { get; set; }

		private readonly ServerSettings _serverSettings;

		public ReportGeneratorService
		(
			ServerSettings serverSettings
		)
		{
			_serverSettings = serverSettings;

			var reportGeneratorLibVersion = typeof(FccLightReportBuilder).BaseType.Assembly.GetName().Version.ToString().Split('.').Take(3);
			MimimumReportGeneratorVersion = Version.Parse(string.Join(".", reportGeneratorLibVersion));
		}

		public void Initialize()
		{
			AppDataReportGeneratorFolder = Path.Combine(_serverSettings.AppDataFolder, "reportGenerator");
			
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

		public Version GetReportGeneratorVersion()
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

		public void UpdateReportGenerator()
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

		public void InstallReportGenerator()
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

		public async Task<(string UnifiedHtmlFile, string UnifiedXmlFile)> RunReportGeneratorAsync(IEnumerable<string> coverOutputFiles, bool darkMode)
		{
			var title = "ReportGenerator Run";
			var outputFolder = Path.GetDirectoryName(coverOutputFiles.OrderBy(x => x).First()); // use location of first file to output reports

			Directory.GetFiles(outputFolder, "*.htm*").ToList().ForEach(File.Delete); // delete html files if they exist

			var unifiedHtmlFile = Path.Combine(outputFolder, "index.html");
			var unifiedXmlFile = Path.Combine(outputFolder, "Cobertura.xml");

			var reportGeneratorSettings = new List<string>();

			reportGeneratorSettings.Add($@"""-targetdir:{outputFolder}""");
			
			async Task<bool> runAsync(string outputReportType, string inputReports)
			{
				var reportTypeSettings = reportGeneratorSettings.ToArray().ToList();

				if (outputReportType.Equals("Cobertura", StringComparison.OrdinalIgnoreCase))
				{
					reportTypeSettings.Add($@"""-reports:{inputReports}""");
					reportTypeSettings.Add($@"""-reporttypes:Cobertura""");
				}
				else if (outputReportType.Equals("HtmlInline_AzurePipelines", StringComparison.OrdinalIgnoreCase))
				{
					reportTypeSettings.Add($@"""-reports:{inputReports}""");
					reportTypeSettings.Add($@"""-plugins:{typeof(FccLightReportBuilder).Assembly.Location}""");
					reportTypeSettings.Add($@"""-reporttypes:{(darkMode ? FccDarkReportBuilder.REPORT_TYPE : FccLightReportBuilder.REPORT_TYPE)}""");
				}
				else
				{
					throw new Exception($"Unknown reporttype '{outputReportType}'");
				}

				Logger.Log($"{title} Arguments [reporttype:{outputReportType}] {Environment.NewLine}{string.Join($"{Environment.NewLine}", reportTypeSettings)}");

				var result = await ProcessUtil.ExecuteAsync(
					FilePath: ReportGeneratorExePath,
					Arguments: string.Join(" ", reportTypeSettings),
					WorkingDirectory: outputFolder
				);

				if (result.ExitCode != 0)
				{
					if (throwError)
					{
						throw new Exception(result.Output);
					}

					Logger.Log($"{title} [reporttype:{outputReportType}] Error", result.Output);
					return false;
				}

				Logger.Log($"{title} [reporttype:{outputReportType}]", result.Output);
				return true;
			}

			if (!await runAsync("Cobertura", string.Join(";", coverOutputFiles)))
			{
				return false;
			}

			if (!await runAsync("HtmlInline_AzurePipelines", unifiedXmlFile))
			{
				return false;
			}

			//return true;

			return (
				UnifiedHtmlFile : unifiedHtmlFile,
				UnifiedXmlFile : unifiedXmlFile
			);
		}

		public async Task<string> ProcessUnifiedHtmlFileAsync(string htmlFile, bool darkMode)
		{
			return await AssemblyUtil.RunInAssemblyResolvingContextAsync(async () =>
			{
				// read [htmlFile] into memory

				var htmlFileContent = await File.ReadAllTextAsync(htmlFile);

				var folder = Path.GetDirectoryName(htmlFile);

				// create and save doc util

				var doc = new HtmlDocument();

				doc.OptionFixNestedTags = true;
				doc.OptionAutoCloseOnEnd = true;

				doc.LoadHtml(htmlFileContent);

				doc.DocumentNode.QuerySelectorAll(".footer").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
				doc.DocumentNode.QuerySelectorAll(".container").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:0;border:0"));
				doc.DocumentNode.QuerySelectorAll(".containerleft").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:0;border:0"));
				doc.DocumentNode.QuerySelectorAll(".containerleft > h1 , .containerleft > p").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
				
				// DOM changes

				var table = doc.DocumentNode.QuerySelectorAll("table.overview").First();
				var tableRows = table.QuerySelectorAll("tr").ToArray();
				try { tableRows[0].SetAttributeValue("style", "display:none"); } catch { }
				try { tableRows[1].SetAttributeValue("style", "display:none"); } catch { }
				try { tableRows[10].SetAttributeValue("style", "display:none"); } catch { }
				try { tableRows[10].SetAttributeValue("style", "display:none"); } catch { }
				try { tableRows[11].SetAttributeValue("style", "display:none"); } catch { }
				try { tableRows[12].SetAttributeValue("style", "display:none"); } catch { }

				// TEXT changes
				var assemblyClassDelimiter = "!";
				var outerHtml = doc.DocumentNode.OuterHtml;
				var htmlSb = new StringBuilder(outerHtml);
				var assembliesSearch = "var assemblies = [";
				var startIndex = outerHtml.IndexOf(assembliesSearch) + assembliesSearch.Length - 1;
				var endIndex = outerHtml.IndexOf("var historicCoverageExecutionTimes");
				var assembliesToReplace = outerHtml[startIndex..endIndex];
				endIndex = assembliesToReplace.LastIndexOf(']');
				assembliesToReplace = assembliesToReplace.Substring(0, endIndex + 1);
				var assemblies = JArray.Parse(assembliesToReplace);
				foreach (JObject assembly in assemblies)
				{
					var assemblyName = assembly["name"];
					var classes = assembly["classes"] as JArray;

					var autoGeneratedRemovals = new List<JObject>();
					foreach (JObject @class in classes)
					{
						var className = @class["name"].ToString();
						if (className == "AutoGeneratedProgram")
						{
							autoGeneratedRemovals.Add(@class);
						}
						else
						{
							// simplify name
							var lastIndexOfDotInName = className.LastIndexOf('.');
							if (lastIndexOfDotInName != -1) @class["name"] = className[lastIndexOfDotInName..].Trim('.');

							//mark with # and add the assembly name
							var rp = @class["rp"].ToString();
							var htmlIndex = rp.IndexOf(".html");
							@class["rp"] = $"#{assemblyName}{assemblyClassDelimiter}{className + ".html" + rp[(htmlIndex + 5)..]}";
						}

					}
					foreach (var autoGeneratedRemoval in autoGeneratedRemovals)
					{
						classes.Remove(autoGeneratedRemoval);
					}

				}
				var assembliesReplaced = assemblies.ToString();
				htmlSb.Replace(assembliesToReplace, assembliesReplaced);

				htmlSb.Replace(".table-fixed", ".table-fixed-ignore-me");

				htmlSb.Replace("</head>", $@"
					<style type=""text/css"">
						*, body {{ font-size: small; }}
						table td {{ white-space: nowrap; }}
						table.coverage {{ width:150px;height:13px }}
						body {{ padding-left:3px;padding-right:3px;padding-bottom:3px }}
						table,tr,th,td {{ border: 1px solid #3f3f46; font-size: small; }}
						a, a:hover {{ color: #0078D4; text-decoration: none; cursor: pointer; }}
						body {{ -webkit-user-select:none;-moz-user-select:none;-ms-user-select:none;-o-user-select:none;user-select:none }}
						table.overview th, table.overview td {{ font-size: small; white-space: nowrap; word-break: normal; padding-left:10px;padding-right:10px; }}
						coverage-info div.customizebox div:nth-child(2) {{ opacity:0;font-size:1px;height:1px;padding:0;border:0;margin:0 }}
						coverage-info div.customizebox div:nth-child(2) * {{ opacity:0;font-size:1px;height:1px;padding:0;border:0;margin:0 }}
					</style>
					</head>
				");

				if (darkMode)
				{
					htmlSb.Replace("</head>", $@"
						<style type=""text/css"">
							*, body {{ color: #f1f1f1 }}
							table.overview.table-fixed {{ border: 1px solid #3f3f46; }}
							body, html {{ scrollbar-arrow-color:#999;scrollbar-track-color:#3e3e42;scrollbar-face-color:#686868;scrollbar-shadow-color:#686868;scrollbar-highlight-color:#686868;scrollbar-3dlight-color:#686868;scrollbar-darkshadow-Color:#686868; }}
						</style>
						</head>
					");
				}
				else
				{
					htmlSb.Replace("</head>", $@"
						<style type=""text/css"">
							table.overview.table-fixed {{ border-width: 1px }}
						</style>
						</head>
					");
				}

				htmlSb.Replace("</body>", $@"
					<script type=""text/javascript"">
						
						var htmlExtension = '.html';
						
						var eventListener = function (element, event, func) {{
							if (element.addEventListener)
								element.addEventListener(event, func, false);
							else if (element.attachEvent)
								element.attachEvent('on' + event, func);
							else
								element['on' + event] = func;
						}};

						var classes = {{}};
						
						Array.prototype.forEach.call(assemblies, function (assembly) {{
							setTimeout(function () {{
								Array.prototype.forEach.call(assembly.classes, function (classs) {{
									setTimeout(function () {{
										classs.assembly = assembly;
										classes[classs.rp] = classs;
									}});
								}});
							}});
						}});
						
						eventListener(document, 'click', function (event) {{
							
							var target = event.target;
							if (target.tagName.toLowerCase() !== 'a') return;
							
							var href = target.getAttribute('href');
							if (!href || href[0] !== '#') return;
							
							var htmlExtensionIndex = href.toLowerCase().indexOf(htmlExtension);
							if (htmlExtensionIndex === -1) return;
							
							if (event.preventDefault) event.preventDefault()
							if (event.stopPropagation) event.stopPropagation();
							
							var assemblyAndQualifiedClassName = href.substring(1, htmlExtensionIndex);
							var delimiterIndex = assemblyAndQualifiedClassName.indexOf('{assemblyClassDelimiter}');
							var assembly = assemblyAndQualifiedClassName.substring(0, delimiterIndex);
							var qualifiedClassName = assemblyAndQualifiedClassName.substring(delimiterIndex + 1);
							var fileLine = href.substring(htmlExtensionIndex + htmlExtension.length);
							
							if (fileLine.indexOf('#') !== -1)
								fileLine = fileLine.substring(fileLine.indexOf('#') + 1).replace('file', '').replace('line', '').split('_');
							else
								fileLine = ['0', '0'];
							
							window.external.OpenFile(assembly, qualifiedClassName, parseInt(fileLine[0]), parseInt(fileLine[1]));
							
							return false;
						}});
							
					</script>
					</body>
				");

				htmlSb.Replace("</head>", $@"
					<style type=""text/css"">
						table.overview.table-fixed.stripped > thead > tr > th:nth-of-type(4) > a:nth-of-type(2) {{ display: none; }}
					</style>
					</head>
				");

				if (darkMode)
				{
					htmlSb.Replace("<body>", @"
						<body>
						<style>
							#divHeader {
								background-color: #252526;
							}
							table#headerTabs td {
								color: #969696;
								border-color:#969696;
							}
						</style>
					");
				}
				else
				{
					htmlSb.Replace("<body>", @"
						<body>
						<style>
							#divHeader {
								background-color: #ffffff;
							}
							table#headerTabs td {
								color: #3b3b3b;
								border-color: #3b3b3b;
							}
						</style>
					");
				}

				htmlSb.Replace("<body>", @"
					<body oncontextmenu='return false;'>
					<style>
						
						table#headerTabs td {
							border-width:3px;
							padding: 3px;
							padding-left: 7px;
							padding-right: 7px;
						}
						table#headerTabs td.tab {
							cursor: pointer;
						}
						table#headerTabs td.active {
							border-bottom: 3px solid transparent;
							font-weight: bolder;
						}
						
					</style>
					<script>
					
						var body = document.getElementsByTagName('body')[0];
						body.style['padding-top'] = '50px';
					
						var tabs = [
							{ button: 'btnCoverage', content: 'coverage-info' }, 
							{ button: 'btnSummary', content: 'table-fixed' },
							{ button: 'btnRiskHotspots', content: 'risk-hotspots' },
						];
					
						var openTab = function (tabIndex) {
							for (var i = 0; i < tabs.length; i++) {
							
								var tab = tabs[i];
								if (!tab) continue;
							
								var button = document.getElementById(tab.button);
								if (!button) continue;
							
								var content = document.getElementsByTagName(tab.content)[0];
								if (!content) content = document.getElementsByClassName(tab.content)[0];
								if (!content) continue;
							
								if (i == tabIndex) {
									if (button.className.indexOf('active') == -1) button.className += ' active';
									content.style.display = 'block';
								} else {
									button.className = button.className.replace('active', '');
									content.style.display = 'none';
								}
							}
						};
					
						window.addEventListener('load', function() {
							openTab(0);
						});
					
					</script>
					<div id='divHeader' style='border-collapse:collapse;padding:0;padding-top:3px;margin:0;border:0;position:fixed;top:0;left:0;width:100%;z-index:100' cellpadding='0' cellspacing='0'>
						<table id='headerTabs' style='border-collapse:collapse;padding:0;margin:0;border:0' cellpadding='0' cellspacing='0'>
							<tr style='padding:0;margin:0;border:0;'>
								<td style='width:3px;white-space:no-wrap;padding-left:0;padding-right:0;border-left:0;border-top:0'>
								</td>
								<td id='btnCoverage' onclick='return openTab(0);' class='tab' style='width:1%;white-space:no-wrap;'>
									Coverage
								</td>
								<td id='btnSummary' onclick='return openTab(1);' class='tab' style='width:1%;white-space:no-wrap'>
									Summary
								</td>
								<td id='btnRiskHotspots' onclick='return openTab(2);' class='tab' style='width:1%;white-space:no-wrap'>
									Risk Hotspots
								</td>
								<td style='border-top:transparent;border-right:transparent;padding-top:0px' align='center'>
									<a href='#' onclick='return window.external.RateAndReview();' style='margin-right:7px'>Rate & Review</a>
									<a href='#' onclick='return window.external.LogIssueOrSuggestion();' style='margin-left:7px'>Log Issue/Suggestion</a>
								</td>
								<td style='width:1%;white-space:no-wrap;border-top:transparent;border-right:transparent;border-left:transparent;padding-top:0px'>
									<a href='#' onclick='return window.external.BuyMeACoffee();'>Buy me a coffee</a>
								</td>
							</tr>
						</table>
					</div>
				");

				htmlSb.Replace("branchCoverageAvailable = true", "branchCoverageAvailable = false");

				var html = string.Join(
				Environment.NewLine,
				htmlSb.ToString().Split('\r', '\n')
				.Select(line =>
				{
					// modify column widths

					if (line.StartsWith(".column"))
					{
						line = $"{line.Substring(0, line.IndexOf('{')).Trim('{')} {{white-space: nowrap; width:1%;}}";
					}

					// modify coverage data

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
							if (lastIndexOfDotInName != -1) lineJO["name"] = name[lastIndexOfDotInName..].Trim('.');

							// prefix the url with #

							lineJO["rp"] = $"#{lineJO.Value<string>("rp")}";

							// output line

							line = $"{lineJO.ToString(Formatting.None)},";
						}
					}

					// modify risk host spots data

					if (line.IndexOf(@"""assembly"":") != -1 && line.IndexOf(@"""class"":") != -1 && line.IndexOf(@"""reportPath"":") != -1)
					{
						var lineJO = JObject.Parse($"{{ {line.TrimEnd(',')} }}");

						// simplify class

						var _class = lineJO.Value<string>("class");
						var lastIndexOfDotInClass = _class.LastIndexOf('.');
						if (lastIndexOfDotInClass != -1) lineJO["class"] = _class[lastIndexOfDotInClass..].Trim('.');

						// prefix the urls with #

						lineJO["reportPath"] = $"#{lineJO.Value<string>("reportPath")}";

						// output line

						line = $"{lineJO.ToString(Formatting.None).Trim('{', '}')},";
					}

					return line;
				}));

				// save

				var resultHtmlFile = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(htmlFile)}-processed{Path.GetExtension(htmlFile)}");
				File.WriteAllText(resultHtmlFile, html);
				return resultHtmlFile;
			});
		}
	}
}
