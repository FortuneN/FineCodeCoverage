using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportGeneratorPlugins;

namespace FineCodeCoverage.Engine.ReportGenerator
{
	interface IReportGeneratorUtil
    {
		void Initialize(string appDataFolder);
		string ProcessUnifiedHtml(string htmlForProcessing,string reportOutputFolder, bool darkMode);
		Task<ReportGeneratorResult> GenerateAsync(IEnumerable<string> coverOutputFiles,string reportOutputFolder, bool darkMode, bool throwError = false);

	}

	internal class ReportGeneratorResult
	{
		public string UnifiedHtml { get; set; }
		public string UnifiedXmlFile { get; set; }
		public bool Success { get; set; }
	}

	[Export(typeof(IReportGeneratorUtil))]
	internal partial class ReportGeneratorUtil: IReportGeneratorUtil
	{
        private readonly IAssemblyUtil assemblyUtil;
        private readonly IProcessUtil processUtil;
        private readonly ILogger logger;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
		private readonly IFileUtil fileUtil;
        private readonly IAppOptionsProvider appOptionsProvider;
        private const string zipPrefix = "reportGenerator";
		private const string zipDirectoryName = "reportGenerator";

        public string ReportGeneratorExePath { get; private set; }

		[ImportingConstructor]
		public ReportGeneratorUtil(
			IAssemblyUtil assemblyUtil,
			IProcessUtil processUtil, 
			ILogger logger,
			IToolFolder toolFolder,
			IToolZipProvider toolZipProvider,
			IFileUtil fileUtil,
			IAppOptionsProvider appOptionsProvider
			)
		{
			this.fileUtil = fileUtil;
            this.appOptionsProvider = appOptionsProvider;
            this.assemblyUtil = assemblyUtil;
            this.processUtil = processUtil;
            this.logger = logger;
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
        }

		public void Initialize(string appDataFolder)
		{
			var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix));
			ReportGeneratorExePath = Directory.GetFiles(zipDestination, "reportGenerator.exe", SearchOption.AllDirectories).FirstOrDefault()
								  ?? Directory.GetFiles(zipDestination, "*reportGenerator*.exe", SearchOption.AllDirectories).FirstOrDefault();
		}

		public async Task<ReportGeneratorResult> GenerateAsync(IEnumerable<string> coverOutputFiles,string reportOutputFolder, bool darkMode, bool throwError = false)
		{
			var title = "ReportGenerator Run";

			var unifiedHtmlFile = Path.Combine(reportOutputFolder, "index.html");
			var unifiedXmlFile = Path.Combine(reportOutputFolder, "Cobertura.xml");

			var reportGeneratorSettings = new List<string>();

			reportGeneratorSettings.Add($@"""-targetdir:{reportOutputFolder}""");
			
			async Task<bool> run(string outputReportType, string inputReports)
			{
				var reportTypeSettings = reportGeneratorSettings.ToArray().ToList();

				if (outputReportType.Equals("Cobertura", StringComparison.OrdinalIgnoreCase))
				{
					reportTypeSettings.Add($@"""-reports:{inputReports}""");
					reportTypeSettings.Add($@"""-reporttypes:Cobertura""");
					var options = appOptionsProvider.Get();
					var cyclomaticThreshold = options.ThresholdForCyclomaticComplexity;
					var crapScoreThreshold = options.ThresholdForCrapScore;
					var nPathThreshold = options.ThresholdForNPathComplexity;
					
					reportTypeSettings.Add($@"""-riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity={cyclomaticThreshold}""");
					reportTypeSettings.Add($@"""-riskHotspotsAnalysisThresholds:metricThresholdForCrapScore={crapScoreThreshold}""");
					reportTypeSettings.Add($@"""-riskHotspotsAnalysisThresholds:metricThresholdForNPathComplexity={nPathThreshold}""");

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

				logger.Log($"{title} Arguments [reporttype:{outputReportType}] {Environment.NewLine}{string.Join($"{Environment.NewLine}", reportTypeSettings)}");

				var result = await processUtil
					.ExecuteAsync(new ExecuteRequest
					{
						FilePath = ReportGeneratorExePath,
						Arguments = string.Join(" ", reportTypeSettings),
						WorkingDirectory = reportOutputFolder
					});
				

				if(result != null)
                {
					if (result.ExitCode != 0)
					{
						logger.Log($"{title} [reporttype:{outputReportType}] Error", result.Output);
						logger.Log($"{title} [reporttype:{outputReportType}] Error", result.ExitCode);

						if (throwError)
						{
							throw new Exception(result.Output);
						}

						return false;
					}

					logger.Log($"{title} [reporttype:{outputReportType}]", result.Output);
					return true;
				}
				return false;
				
			}
			
			var reportGeneratorResult = new ReportGeneratorResult { Success = false, UnifiedHtml = null, UnifiedXmlFile = unifiedXmlFile };
			
			var coberturaResult = await run("Cobertura", string.Join(";", coverOutputFiles));

			if (coberturaResult)
			{
				var htmlResult = await run("HtmlInline_AzurePipelines", unifiedXmlFile);
				if (htmlResult)
				{
					reportGeneratorResult.UnifiedHtml = fileUtil.ReadAllText(unifiedHtmlFile);
					reportGeneratorResult.Success = true;
                }
				
			}

			return reportGeneratorResult;
			
		}

		public string ProcessUnifiedHtml(string htmlForProcessing, string reportOutputFolder, bool darkMode)
		{
			return assemblyUtil.RunInAssemblyResolvingContext(() =>
			{
				var doc = new HtmlDocument();

				doc.OptionFixNestedTags = true;
				doc.OptionAutoCloseOnEnd = true;

				doc.LoadHtml(htmlForProcessing);
				htmlForProcessing = null;

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
				var assembliesToReplace = outerHtml.Substring(startIndex, endIndex - startIndex);
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
							if (lastIndexOfDotInName != -1) @class["name"] = className.Substring(lastIndexOfDotInName).Trim('.');

							//mark with # and add the assembly name
							var rp = @class["rp"].ToString();
							var htmlIndex = rp.IndexOf(".html");
							@class["rp"] = $"#{assemblyName}{assemblyClassDelimiter}{className + ".html" + rp.Substring(htmlIndex + 5)}";
						}

					}
					foreach (var autoGeneratedRemoval in autoGeneratedRemovals)
					{
						classes.Remove(autoGeneratedRemoval);
					}

				}
				var assembliesReplaced = assemblies.ToString();
				htmlSb.Replace(assembliesToReplace, assembliesReplaced);

                //is this even present if there are no riskhotspots
                var riskHotspotsSearch = "var riskHotspots = [";
                var rhStartIndex = outerHtml.IndexOf(riskHotspotsSearch) + riskHotspotsSearch.Length - 1;
                var rhEndIndex = outerHtml.IndexOf("var branchCoverageAvailable");
                var rhToReplace = outerHtml.Substring(rhStartIndex, rhEndIndex - rhStartIndex);
                rhEndIndex = rhToReplace.LastIndexOf(']');
                rhToReplace = rhToReplace.Substring(0, rhEndIndex + 1);

                var riskHotspots = JArray.Parse(rhToReplace);
                foreach (JObject riskHotspot in riskHotspots)
                {
                    var assembly = riskHotspot["assembly"].ToString();
                    var qualifiedClassName = riskHotspot["class"].ToString();
					// simplify name
					var lastIndexOfDotInName = qualifiedClassName.LastIndexOf('.');
					if (lastIndexOfDotInName != -1) riskHotspot["class"] = qualifiedClassName.Substring(lastIndexOfDotInName).Trim('.');
					var newReportPath = $"#{assembly}{assemblyClassDelimiter}{qualifiedClassName}.html";
                    riskHotspot["reportPath"] = newReportPath;
                }
                var riskHotspotsReplaced = riskHotspots.ToString();
                htmlSb.Replace(rhToReplace, riskHotspotsReplaced);

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

						eventListener(window,'focus',function(){{window.external.DocumentFocused()}});

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
								fileLine = ['-1', '0'];
							
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

						var riskHotspotsTable;
						var riskHotspotsElement;
						var addedFileIndexToRiskHotspots = false;
						var addFileIndexToRiskHotspotsClassLink = function(){
						  if(!addedFileIndexToRiskHotspots){
							addedFileIndexToRiskHotspots = true;
							var riskHotspotsElements = document.getElementsByTagName('risk-hotspots');
							if(riskHotspotsElements.length == 1){{
								riskHotspotsElement = riskHotspotsElements[0];
								riskHotspotsTable = riskHotspotsElement.querySelector('table');
								if(riskHotspotsTable){
									var rhBody = riskHotspotsTable.querySelector('tbody');
									var rows = rhBody.rows;
									for(var i=0;i<rows.length;i++){
									  var row = rows[i];
									  var cells = row.cells;
									  var classCell = cells[1];
									  var classLink = classCell.children[0];
									  var methodCell = cells[2];
									  var classLink = classCell.children[0];
									  var methodLink = methodCell.children[0];
									  var methodHash = methodLink.hash;
									  var methodHtmlIndex = methodHash.indexOf('.html');
									  var fileLine = methodHash.substring(methodHtmlIndex + 6);
									  var fileAndLine = fileLine.replace('file', '').replace('line', '').split('_');
									  var file = fileAndLine[0];
									  var line = fileAndLine[1];
									  classLink.href = classLink.hash + '#file' + file + '_line0';
									}
								}
								
							}}
							}
						}
						
						// necessary for WebBrowser 
						function removeElement(element){
							element.parentNode.removeChild(element);
						}

						function insertAfter(newNode, existingNode) {
							existingNode.parentNode.insertBefore(newNode, existingNode.nextSibling);
						}

						var noHotspotsMessage
						var addNoRiskHotspotsMessageIfRequired = function(){
							if(riskHotspotsTable == null){
								noHotspotsMessage = document.createElement(""p"");
								noHotspotsMessage.style.margin = ""0"";
								noHotspotsMessage.innerText = ""No risk hotspots found."";

								insertAfter(noHotspotsMessage, riskHotspotsElement);
							}
						}

						var removeNoRiskHotspotsMessage = function(){
							if(noHotspotsMessage){
								removeElement(noHotspotsMessage);
								noHotspotsMessage = null;
							}
						}

						var openTab = function (tabIndex) {
							if(tabIndex==2){{
								addFileIndexToRiskHotspotsClassLink();
								addNoRiskHotspotsMessageIfRequired();
							}}else{{
								removeNoRiskHotspotsMessage();
							}}
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

				var processed = string.Join(
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
							if (lastIndexOfDotInName != -1) lineJO["name"] = name.Substring(lastIndexOfDotInName).Trim('.');

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
						if (lastIndexOfDotInClass != -1) lineJO["class"] = _class.Substring(lastIndexOfDotInClass).Trim('.');

						// prefix the urls with #

						lineJO["reportPath"] = $"#{lineJO.Value<string>("reportPath")}";

						// output line

						line = $"{lineJO.ToString(Formatting.None).Trim('{', '}')},";
					}

					return line;
				}));

				var processedHtmlFile = Path.Combine(reportOutputFolder, "index-processed.html");
				File.WriteAllText(processedHtmlFile, processed);

				return processed;

			});
		}
	}
}
