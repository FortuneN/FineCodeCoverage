using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Windows;
using ExCSS;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using FineCodeCoverage.Output;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportGeneratorPlugins;
using System.Threading;
using System.Xml.Linq;
using FineCodeCoverage.Impl;

namespace FineCodeCoverage.Engine.ReportGenerator
{
	interface IReportGeneratorUtil
    {
        void Initialize(string appDataFolder, CancellationToken cancellationToken);
		string ProcessUnifiedHtml(string htmlForProcessing,string reportOutputFolder);
		Task<ReportGeneratorResult> GenerateAsync(IEnumerable<string> coverOutputFiles,string reportOutputFolder,CancellationToken cancellationToken);
        string BlankReport(bool withHistory);
        void LogCoverageProcess(string message);
		void EndOfCoverageRun();
    }

    internal class ReportGeneratorResult
	{
		public string UnifiedHtml { get; set; }
		public string UnifiedXmlFile { get; set; }
		public string HotspotsFile { get; set; }
	}

	[Export(typeof(IReportGeneratorUtil))]
	internal partial class ReportGeneratorUtil : 
		IReportGeneratorUtil, 
		IListener<EnvironmentFontDetailsChangedMessage>, IListener<DpiChangedMessage>, IListener<ReadyForReportMessage>
	{
		private readonly IAssemblyUtil assemblyUtil;
		private readonly IProcessUtil processUtil;
		private readonly ILogger logger;
        private readonly IToolUnzipper toolUnzipper;
        private readonly IReportColoursProvider reportColoursProvider;
        private readonly IFileUtil fileUtil;
		private readonly IAppOptionsProvider appOptionsProvider;
		private readonly IResourceProvider resourceProvider;
        private readonly IShowFCCOutputPane showFCCOutputPane;
        private readonly IEventAggregator eventAggregator;
        private const string zipPrefix = "reportGenerator";
		private const string zipDirectoryName = "reportGenerator";

		private const string CoverageLogJSFunctionName = "coverageLog";
		private const string CoverageLogTabName = "Coverage Log";
		private const string ShowFCCWorkingJSFunctionName = "showFCCWorking";
		private const string FontChangedJSFunctionName = "fontChanged";
		private readonly Base64ReportImage plusBase64ReportImage = new Base64ReportImage(".icon-plus", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPHN2ZyB3aWR0aD0iMTc5MiIgaGVpZ2h0PSIxNzkyIiB2aWV3Qm94PSIwIDAgMTc5MiAxNzkyIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0xNjAwIDczNnYxOTJxMCA0MC0yOCA2OHQtNjggMjhoLTQxNnY0MTZxMCA0MC0yOCA2OHQtNjggMjhoLTE5MnEtNDAgMC02OC0yOHQtMjgtNjh2LTQxNmgtNDE2cS00MCAwLTY4LTI4dC0yOC02OHYtMTkycTAtNDAgMjgtNjh0NjgtMjhoNDE2di00MTZxMC00MCAyOC02OHQ2OC0yOGgxOTJxNDAgMCA2OCAyOHQyOCA2OHY0MTZoNDE2cTQwIDAgNjggMjh0MjggNjh6Ii8+PC9zdmc+");
		private readonly Base64ReportImage minusBase64ReportImage = new Base64ReportImage(".icon-minus", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjxzdmcgd2lkdGg9IjE3OTIiIGhlaWdodD0iMTc5MiIgdmlld0JveD0iMCAwIDE3OTIgMTc5MiIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cGF0aCBmaWxsPSIjMDAwIiBkPSJNMTYwMCA3MzZ2MTkycTAgNDAtMjggNjh0LTY4IDI4aC0xMjE2cS00MCAwLTY4LTI4dC0yOC02OHYtMTkycTAtNDAgMjgtNjh0NjgtMjhoMTIxNnE0MCAwIDY4IDI4dDI4IDY4eiIvPjwvc3ZnPg==");
		private readonly Base64ReportImage downActiveBase64ReportImage = new Base64ReportImage(".icon-down-dir_active", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjxzdmcgd2lkdGg9IjE3OTIiIGhlaWdodD0iMTc5MiIgdmlld0JveD0iMCAwIDE3OTIgMTc5MiIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cGF0aCBmaWxsPSIjMDA3OEQ0IiBkPSJNMTQwOCA3MDRxMCAyNi0xOSA0NWwtNDQ4IDQ0OHEtMTkgMTktNDUgMTl0LTQ1LTE5bC00NDgtNDQ4cS0xOS0xOS0xOS00NXQxOS00NSA0NS0xOWg4OTZxMjYgMCA0NSAxOXQxOSA0NXoiLz48L3N2Zz4=");
		private readonly Base64ReportImage downInactiveBase64ReportImage = new Base64ReportImage(".icon-down-dir", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPHN2ZyB3aWR0aD0iMTc5MiIgaGVpZ2h0PSIxNzkyIiB2aWV3Qm94PSIwIDAgMTc5MiAxNzkyIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0xNDA4IDcwNHEwIDI2LTE5IDQ1bC00NDggNDQ4cS0xOSAxOS00NSAxOXQtNDUtMTlsLTQ0OC00NDhxLTE5LTE5LTE5LTQ1dDE5LTQ1IDQ1LTE5aDg5NnEyNiAwIDQ1IDE5dDE5IDQ1eiIvPjwvc3ZnPg==");
		private readonly Base64ReportImage upActiveBase64ReportImage = new Base64ReportImage(".icon-up-dir_active", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjxzdmcgd2lkdGg9IjE3OTIiIGhlaWdodD0iMTc5MiIgdmlld0JveD0iMCAwIDE3OTIgMTc5MiIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cGF0aCBmaWxsPSIjMDA3OEQ0IiBkPSJNMTQwOCAxMjE2cTAgMjYtMTkgNDV0LTQ1IDE5aC04OTZxLTI2IDAtNDUtMTl0LTE5LTQ1IDE5LTQ1bDQ0OC00NDhxMTktMTkgNDUtMTl0NDUgMTlsNDQ4IDQ0OHExOSAxOSAxOSA0NXoiLz48L3N2Zz4=");
        private readonly IScriptManager scriptManager;
		private DpiScale dpiScale;
		private FontDetails environmentFontDetails;
		private string previousFontSizeName;
		private string unprocessedReport;
		private string previousReportOutputFolder;
        private IReportColours reportColours;
		private JsThemeStyling jsReportColours;
		private IReportColours ReportColours
        {
			get => reportColours;
            set
            {
				reportColours = value;
				jsReportColours = reportColours.Convert();
            }
        }
		private readonly bool showBranchCoverage = true;
		private readonly List<string> logs = new List<string>();

		public string ReportGeneratorExePath { get; private set; }

		private string FontSize => environmentFontDetails == null ? "12px" : $"{environmentFontDetails.Size * dpiScale.DpiScaleX}px";
		private string FontName => environmentFontDetails == null ? "Arial" : environmentFontDetails.Family.Source;
		private readonly HotspotReader hotspotsReader = new HotspotReader();

		[ImportingConstructor]
		public ReportGeneratorUtil(
			IAssemblyUtil assemblyUtil,
			IProcessUtil processUtil,
			ILogger logger,
			IToolUnzipper toolUnzipper,
			IFileUtil fileUtil,
			IAppOptionsProvider appOptionsProvider,
			IReportColoursProvider reportColoursProvider,
			IScriptManager scriptManager,
			IResourceProvider resourceProvider,
			IShowFCCOutputPane showFCCOutputPane,
			IEventAggregator eventAggregator
			)
		{
			this.fileUtil = fileUtil;
			this.appOptionsProvider = appOptionsProvider;
			this.assemblyUtil = assemblyUtil;
			this.processUtil = processUtil;
			this.logger = logger;
            this.toolUnzipper = toolUnzipper;
            this.reportColoursProvider = reportColoursProvider;
            this.reportColoursProvider.ColoursChanged += ReportColoursProvider_ColoursChanged;
			this.scriptManager = scriptManager;
            this.resourceProvider = resourceProvider;
            this.showFCCOutputPane = showFCCOutputPane;
            this.eventAggregator = eventAggregator;
			this.eventAggregator.AddListener(this);
            scriptManager.ClearFCCWindowLogsEvent += ScriptManager_ClearFCCWindowLogsEvent;
            scriptManager.ShowFCCOutputPaneEvent += ScriptManager_ShowFCCOutputPaneEvent;
        }

        private void ScriptManager_ShowFCCOutputPaneEvent(object sender, EventArgs e)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(() => showFCCOutputPane.ShowAsync());
        }

		private void ScriptManager_ClearFCCWindowLogsEvent(object sender, EventArgs e)
        {
			logs.Clear();
        }

        public void Initialize(string appDataFolder, CancellationToken cancellationToken)
		{
			var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolder, zipDirectoryName, zipPrefix, cancellationToken);
			ReportGeneratorExePath = Directory.GetFiles(zipDestination, "reportGenerator.exe", SearchOption.AllDirectories).FirstOrDefault()
								  ?? Directory.GetFiles(zipDestination, "*reportGenerator*.exe", SearchOption.AllDirectories).FirstOrDefault();
		}

		public async Task<ReportGeneratorResult> GenerateAsync(IEnumerable<string> coverOutputFiles, string reportOutputFolder, CancellationToken cancellationToken)
		{
			var title = "ReportGenerator Run";

			var unifiedHtmlFile = Path.Combine(reportOutputFolder, "index.html");
			var unifiedXmlFile = Path.Combine(reportOutputFolder, "Cobertura.xml");
		
			var reportGeneratorSettings = new List<string>();

			reportGeneratorSettings.Add($@"""-targetdir:{reportOutputFolder}""");

			async Task RunAsync(string outputReportType, string inputReports)
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
					reportTypeSettings.Add($@"""-reporttypes:{FccLightReportBuilder.REPORT_TYPE}""");
					var (cyclomaticThreshold, crapScoreThreshold, nPathThreshold) = HotspotThresholds(appOptionsProvider.Get());

					reportTypeSettings.Add($@"""riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity={cyclomaticThreshold}""");
					reportTypeSettings.Add($@"""riskHotspotsAnalysisThresholds:metricThresholdForCrapScore={crapScoreThreshold}""");
					reportTypeSettings.Add($@"""riskHotspotsAnalysisThresholds:metricThresholdForNPathComplexity={nPathThreshold}""");

				}
				else
				{
					throw new Exception($"Unknown reporttype '{outputReportType}'");
				}

				var result = await processUtil
					.ExecuteAsync(new ExecuteRequest
					{
						FilePath = ReportGeneratorExePath,
						Arguments = string.Join(" ", reportTypeSettings),
						WorkingDirectory = reportOutputFolder
					},cancellationToken);


				if (result.ExitCode != 0)
				{
					logger.Log($"{title} [reporttype:{outputReportType}] Error", result.Output, $"ExitCode : {result.ExitCode}");

					throw new Exception(result.Output);
				}

				logger.Log($"{title} [reporttype:{outputReportType}] Output", result.Output);

			}

			var reportGeneratorResult = new ReportGeneratorResult { UnifiedHtml = null, UnifiedXmlFile = unifiedXmlFile };

			var startTime = DateTime.Now;
			LogCoverageProcess("Generating cobertura report");
			await RunAsync("Cobertura", string.Join(";", coverOutputFiles));
			var duration = DateTime.Now - startTime;

			var coberturaDurationMesage = $"Cobertura report generation duration - {duration}";
			LogCoverageProcess(coberturaDurationMesage); // result output includes duration for normal log

			startTime = DateTime.Now;
			LogCoverageProcess("Generating html report");
			await RunAsync("HtmlInline_AzurePipelines", unifiedXmlFile);
			duration = DateTime.Now - startTime;
			cancellationToken.ThrowIfCancellationRequested();
			var htmlReportDurationMessage = $"Html report generation duration - {duration}";
			LogCoverageProcess(htmlReportDurationMessage); // result output includes duration for normal log
			reportGeneratorResult.UnifiedHtml = fileUtil.ReadAllText(unifiedHtmlFile);

            var doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };
            doc.LoadHtml(reportGeneratorResult.UnifiedHtml);
            var hotspots = hotspotsReader.Read(doc);
            var hotspotsFile = WriteHotspotsToOutputFolder(hotspots, reportOutputFolder);
			reportGeneratorResult.HotspotsFile = hotspotsFile;
            return reportGeneratorResult;
		}

		private string GetRowHoverLinkColour(bool useLightness = true)
		{
			if (useLightness)
			{
				return LightenssApplier.Swap(
					ReportColoursExtensions.ToColor(jsReportColours.CoverageTableRowHoverColour),
					ReportColoursExtensions.ToColor(jsReportColours.LinkColour)
				).ToJsColour();
			}

			return jsReportColours.CoverageTableRowHoverColour;
        }

		private void SetInitialTheme(HtmlAgilityPack.HtmlDocument document)
		{
			var backgroundColor = jsReportColours.BackgroundColour;
			var fontColour = jsReportColours.FontColour;
			var overviewTableBorderColor = jsReportColours.TableBorderColour;

			var style = document.DocumentNode.Descendants("style").First();

			var parser = new StylesheetParser();
			var stylesheet = parser.Parse(style.InnerHtml);
			var styleRules = stylesheet.StyleRules;

			var lightRedForCyclomatic = styleRules.First(r => r.SelectorText == ".lightred");
			lightRedForCyclomatic.Style.BackgroundColor = null;
			/*
                Other option
                lightRedForCyclomatic.Style.BackgroundColor = "rgba(255,255,255,0.3)";

                or 
                be *dynamic* and be lighten / darken %age of the background color
            */
			var grayRule = styleRules.First(r => r.SelectorText == ".gray");
			grayRule.Style.BackgroundColor = jsReportColours.GrayCoverageColour;

			var htmlRule = styleRules.First(r => r.Selector.Text == "html");
			var htmlStyle = htmlRule.Style;
			htmlStyle.BackgroundColor = backgroundColor;

			var containerRule = styleRules.First(r => r.SelectorText == ".container");
			containerRule.Style.BackgroundColor = backgroundColor;

			var overviewTableBorder = $"1px solid {overviewTableBorderColor}";
			var overviewThRule = styleRules.First(r => r.SelectorText == ".overview th");
			overviewThRule.Style.BackgroundColor = backgroundColor;
			overviewThRule.Style.Border = overviewTableBorder;

			var overviewTdRule = styleRules.First(r => r.SelectorText == ".overview td");
			overviewTdRule.Style.Border = overviewTableBorder;

			var overviewRule = styleRules.First(r => r.SelectorText == ".overview");
			overviewRule.Style.Border = overviewTableBorder;

			var overviewHeaderLinks = styleRules.First(r => r.SelectorText == ".overview th a");
			overviewHeaderLinks.Style.Color = jsReportColours.CoverageTableHeaderFontColour;

			var overviewTrHoverRule = styleRules.First(r => r.SelectorText == ".overview tr:hover");

            /*
				Alternative is lighten / darken the background color
				ControlPaint.Dark / Light is percentage based - so we can't use it here
			*/
             
            overviewTrHoverRule.Style.Background = jsReportColours.CoverageTableRowHoverBackgroundColour;
            
            var trHoverTdRule = stylesheet.Add(RuleType.Style);
            trHoverTdRule.Text = $".overview tr:hover td {{color:{jsReportColours.CoverageTableRowHoverColour}}}";

			var trHoverARule = stylesheet.Add(RuleType.Style);
            trHoverARule.Text = $".overview tr:hover td a {{color:{GetRowHoverLinkColour()}}}";
            
            var expandCollapseIconColor = ReportColours.CoverageTableExpandCollapseIconColour;
			plusBase64ReportImage.FillSvg(styleRules, expandCollapseIconColor.ToJsColour());
			minusBase64ReportImage.FillSvg(styleRules, expandCollapseIconColor.ToJsColour());

			var coverageTableActiveSortColor = ReportColours.CoverageTableActiveSortColour.ToJsColour();
			var coverageTableInactiveSortColor = ReportColours.CoverageTableInactiveSortColour.ToJsColour();
			downActiveBase64ReportImage.FillSvg(styleRules, coverageTableActiveSortColor);
			upActiveBase64ReportImage.FillSvg(styleRules, coverageTableActiveSortColor);
			downInactiveBase64ReportImage.FillSvg(styleRules, coverageTableInactiveSortColor);

			var linkColor = jsReportColours.LinkColour;
			var linkRule = styleRules.First(r => r.SelectorText == "a");
			var linkHoverRule = styleRules.First(r => r.SelectorText == "a:hover");

			linkRule.Style.Color = linkColor;
			linkRule.Style.Cursor = "pointer";
			linkRule.Style.TextDecoration = "none";

			linkHoverRule.Style.Color = linkColor;
			linkHoverRule.Style.Cursor = "pointer";
			linkHoverRule.Style.TextDecoration = "none";

			var stringWriter = new StringWriter();
			var formatter = new CompressedStyleFormatter();
			stylesheet.ToCss(stringWriter, formatter);
			var changedCss = stringWriter.ToString();
			style.InnerHtml = changedCss;
		}

		private string GetStickyTableHead(IAppOptions appOptions)
		{
            if (!appOptions.StickyCoverageTable)
            {
				return "";
            }
			return @"
function CustomEventPolyfill() {
  if ( typeof window.CustomEvent === 'function' ) return false;

  function CustomEvent( event, params ) {
      params = params || { bubbles: false, cancelable: false, detail: null };
			var evt = document.createEvent('CustomEvent');
			evt.initCustomEvent( event, params.bubbles, params.cancelable, params.detail );
      return evt;
		}

		window.CustomEvent = CustomEvent;
}

	function DataStore()
	{
		return {
		_storage: new WeakMap(),
    put: function(element, key, obj) {
				if (!this._storage.has(element))
				{
					this._storage.set(element, new Map());
				}
				this._storage.get(element).set(key, obj);
			},
    get: function(element, key) {
				let data = this._storage.get(element)

	  return data && data.get(key);
			},
    remove: function(element, key) {
				let data = this._storage.get(element)

	  if (!data) { return }
				let ret = data.delete(key);
				if (!data.size === 0)
				{
					this._storage.delete(element);
				}
				return ret;
			}
		}
	}

	function extendObj(defaultObj, overrideObj)
	{
		var newObj = defaultObj
	  Object.keys(overrideObj || { }).forEach(function(k) {
			newObj[k] = overrideObj[k]
  });

		return newObj
	}

	function getOffset(el)
	{
		var rect = el.getBoundingClientRect();
		return {
		top: rect.top + window.pageYOffset,
    left: rect.left + window.pageXOffset,
  };
	}

	function getHeight(el)
	{
		if (el == window)
		{
			return window.innerHeight
	    }
		if (el == document)
		{
			return Math.max(
			  document.documentElement['clientHeight'],
			  document.body['scrollHeight'],
			  document.documentElement['scrollHeight'],
			  document.body['offsetHeight'],
			  document.documentElement['offsetHeight']
			);
		}
		var height = parseFloat(getComputedStyle(el, null).height.replace('px', ''))
	  return height ? height : el.offsetHeight
	}

	function getWidth(el)
	{
		if (el == window)
		{
			return window.innerWidth
	    }
		if (el == document)
		{
			return Math.max(
			  document.documentElement['clientWidth'],
			  document.body['scrollWidth'],
			  document.documentElement['scrollWidth'],
			  document.body['offsetWidth'],
			  document.documentElement['offsetWidth']
			);
		}
		var width = parseFloat(getComputedStyle(el, null).width.replace('px', ''))
	  return width ? width : el.offsetWidth
	}

	function setStyles(el, propertyObject)
	{
  for (var property in propertyObject)
			el.style[property] = propertyObject[property];
	}

	function fireEvent(name, el, data)
	{
		var details = data ? { } : { details: data }
		var evt = new CustomEvent(name, details)
	  el.dispatchEvent(evt)
	}

	var dataStore = DataStore();
	window.dataStore = dataStore
	CustomEventPolyfill()

function stickytheadapply(elements, options)
	{
		var name = 'stickyThead',
		  id = 0,
		  defaults = {
	  fixedOffset: 0,
      leftOffset: 0,
      marginTop: 0,
      objDocument: document,
      objHead: document.head,
      objWindow: window,
      scrollableArea: window,
      cacheHeaderHeight: false,
      zIndex: 3

	};

	function Plugin(el, options)
	{
		// To avoid scope issues, use 'base' instead of 'this'
		// to reference this class from internal events and functions.
		var base = this;

		// Access to jQuery and DOM versions of element
		// base.$el = $(el);
		base.el = el;
		base.id = id++;

		// Cache DOM refs for performance reasons
		base.$clonedHeader = null;
		base.$originalHeader = null;

		// Cache header height for performance reasons
		base.cachedHeaderHeight = null;

		// Keep track of state
		base.isSticky = false;
		base.hasBeenSticky = false;
		base.leftOffset = null;
		base.topOffset = null;

		base.init = function() {
			base.setOptions(options);

			// base.$el.each(function () {
			// var $this = $(this);

			// remove padding on <table> to fix issue #7
			base.el.style.padding = '0px';

			base.$originalHeader = base.el.querySelector('thead');
			base.$clonedHeader = base.$originalHeader.cloneNode(true);
			// dispatchEvent
			fireEvent('clonedHeader.' + name, base.el, base.$clonedHeader)
   

	  base.$clonedHeader.setAttribute('class', 'tableFloatingHeader');
			setStyles(base.$clonedHeader, { display: 'none', opacity: 0 })

      base.$originalHeader.setAttribute('class', 'tableFloatingHeaderOriginal');

			base.$originalHeader.insertAdjacentElement('afterend', base.$clonedHeader);

			var style = document.createElement('style')
   
	  style.setAttribute('type', 'text/css')
   
	  style.setAttribute('media', 'print')
   
	  style.innerHTML = '.tableFloatingHeader{display:none !important;}' +
		'.tableFloatingHeaderOriginal{position:static !important;}'
   
	  base.$printStyle = style
   
	  base.$head.appendChild(base.$printStyle);


			base.$clonedHeader.querySelectorAll('input, select').forEach(function(el) {
				el.setAttribute('disabled', true);
			})

      base.updateWidth();
			base.toggleHeaders();
			base.bind();
		};

		base.destroy = function() {
			base.el && base.el.removeEventListener('destroyed', base.teardown);
			base.teardown();
		};

		base.teardown = function() {
			if (base.isSticky)
			{
				setStyles(base.$originalHeader, { position: 'static' });
			}
			dataStore.remove(base.el, name)
   
	  base.unbind();

			base.$clonedHeader.parentNode.removeChild(base.$clonedHeader);
			base.$originalHeader.classList.remove('tableFloatingHeaderOriginal');
			setStyles(base.$originalHeader, { visibility: 'visible' });
			base.$printStyle.parentNode.removeChild(base.$printStyle);

			base.el = null;
			base.$el = null;
		};

		base.bind = function() {
			base.$scrollableArea.addEventListener('scroll', base.toggleHeaders);
			if (!base.isWindowScrolling)
			{
				base.$window.addEventListener('scroll', base.setPositionValues);
				base.$window.addEventListener('resize', base.toggleHeaders);
			}
			base.$scrollableArea.addEventListener('resize', base.toggleHeaders);
			base.$scrollableArea.addEventListener('resize', base.updateWidth);
		};

		base.unbind = function() {
			// unbind window events by specifying handle so we don't remove too much
			base.$scrollableArea.removeEventListener('scroll', base.toggleHeaders);
			if (!base.isWindowScrolling)
			{
				base.$window.removeEventListener('scroll', base.setPositionValues);
				base.$window.removeEventListener('resize', base.toggleHeaders);
			}
			base.$scrollableArea.removeEventListener('resize', base.updateWidth);
		};

		base.toggleHeaders = function() {
			if (base.el)
			{
				var newLeft,
				  newTopOffset = base.isWindowScrolling ? (
					isNaN(base.options.fixedOffset) ?
					  base.options.fixedOffset.offsetHeight :
					  base.options.fixedOffset
				  ) :
					getOffset(base.$scrollableArea).top + (!isNaN(base.options.fixedOffset) ? base.options.fixedOffset : 0),
				  offset = getOffset(base.el),

				  scrollTop = base.$scrollableArea.pageYOffset + newTopOffset,
          scrollLeft = base.$scrollableArea.pageXOffset,
          headerHeight,

          scrolledPastTop = base.isWindowScrolling ?
			scrollTop > offset.top :
			newTopOffset > offset.top,
          notScrolledPastBottom;

				if (scrolledPastTop)
				{
					headerHeight = base.options.cacheHeaderHeight ? base.cachedHeaderHeight : getHeight(base.$originalHeader);
					notScrolledPastBottom = (base.isWindowScrolling ? scrollTop : 0) <
					  (offset.top + getHeight(base.el) - headerHeight - (base.isWindowScrolling ? 0 : newTopOffset));
				}

				if (scrolledPastTop && notScrolledPastBottom)
				{
					newLeft = offset.left - scrollLeft + base.options.leftOffset;
					setStyles(base.$originalHeader, {
					position: 'fixed',
            marginTop: base.options.marginTop + 'px',
            top: 0,
            left: newLeft + 'px',
            zIndex: base.options.zIndex
		  
		  });
					base.leftOffset = newLeft;
					base.topOffset = newTopOffset;
					base.$clonedHeader.style.display = '';
					if (!base.isSticky)
					{
						base.isSticky = true;
						// make sure the width is correct: the user might have resized the browser while in static mode
						base.updateWidth();
						fireEvent('enabledStickiness.' + name, base.el)
		  
		  }
					base.setPositionValues();
				}
				else if (base.isSticky)
				{
					base.$originalHeader.style.position = 'static';
					base.$clonedHeader.style.display = 'none';
					base.isSticky = false;
					base.resetWidth(base.$clonedHeader.querySelectorAll('td,th'), base.$originalHeader.querySelectorAll('td,th'));
					fireEvent('disabledStickiness.' + name, base.el)
	  
		}
			}
		};

		base.setPositionValues = function() {
			var winScrollTop = base.$window.pageYOffset,
        winScrollLeft = base.$window.pageXOffset;

			/*if (!base.isSticky ||
			  winScrollTop < 0 || winScrollTop + getHeight(base.$window) > getHeight(base.$document) ||
			  winScrollLeft < 0 || winScrollLeft + getWidth(base.$window) > getWidth(base.$document)) {
			  return;
			}*/
			setStyles(base.$originalHeader, {
			top: base.topOffset - (base.isWindowScrolling ? 0 : winScrollTop) + 'px',
        left: base.leftOffset - (base.isWindowScrolling ? 0 : winScrollLeft) + 'px'
	  
	  });
		};

		base.updateWidth = function() {
			if (!base.isSticky)
			{
				return;
			}
			// Copy cell widths from clone
			if (!base.$originalHeaderCells) {
				base.$originalHeaderCells = base.$originalHeader.querySelectorAll('th,td');
			}
			if (!base.$clonedHeaderCells) {
				base.$clonedHeaderCells = base.$clonedHeader.querySelectorAll('th,td');
			}
			var cellWidths = base.getWidth(base.$clonedHeaderCells);
			base.setWidth(cellWidths, base.$clonedHeaderCells, base.$originalHeaderCells);

			// Copy row width from whole table
			base.$originalHeader.style.width = getWidth(base.$clonedHeader);

			// If we're caching the height, we need to update the cached value when the width changes
			if (base.options.cacheHeaderHeight)
			{
				base.cachedHeaderHeight = getHeight(base.$clonedHeader);
			}
		};

		base.getWidth = function($clonedHeaders) {
			var widths = [];
      $clonedHeaders.forEach(function(el, index) {
				var width;

				if (getComputedStyle(el).boxSizing === 'border-box')
				{
					var boundingClientRect = el.getBoundingClientRect();
					if (boundingClientRect.width)
					{
						width = boundingClientRect.width; // #39: border-box bug
					}
					else
					{
						width = boundingClientRect.right - boundingClientRect.left; // ie8 bug: getBoundingClientRect() does not have a width property
					}
				}
				else
				{
					var $origTh = base.$originalHeader.querySelector('th');
					if ($origTh.style.borderCollapse === 'collapse') {
						if (window.getComputedStyle)
						{
							width = parseFloat(window.getComputedStyle(el, null).width);
						}
						else
						{
							// ie8 only
							var leftPadding = parseFloat(el.style.paddingLeft);
							var rightPadding = parseFloat(el.style.paddingRight);
							// Needs more investigation - this is assuming constant border around this cell and it's neighbours.
							var border = parseFloat(el.style.borderWidth);
							width = el.offsetWidth - leftPadding - rightPadding - border;
						}
					} else
					{
						width = getWidth(el);
					}
				}

				widths[index] = width;
			});
			return widths;
		};

		base.setWidth = function(widths, $clonedHeaders, $origHeaders) {
      $clonedHeaders.forEach(function(_, index) {
				var width = widths[index];
				setStyles($origHeaders[index], {
				minWidth: width + 'px',
          maxWidth: width + 'px'
		
		});
			});
		};

		base.resetWidth = function($clonedHeaders, $origHeaders) {
      $clonedHeaders.forEach(function(_, index) {
				setStyles($origHeaders[index], {
				minWidth: el.style.minWidth,
          maxWidth: el.style.maxWidth
		
		});
			});
		};

		base.setOptions = function(options) {
			base.options = extendObj(defaults, options);
			base.$window = base.options.objWindow;
			base.$head = base.options.objHead;
			base.$document = base.options.objDocument;
			base.$scrollableArea = base.options.scrollableArea;
			base.isWindowScrolling = base.$scrollableArea === base.$window;
		};

		base.updateOptions = function(options) {
			base.setOptions(options);
			// scrollableArea might have changed
			base.unbind();
			base.bind();
			base.updateWidth();
			base.toggleHeaders();
		};

		// Listen for destroyed, call teardown
		base.el.addEventListener('destroyed', base.teardown.bind(base));

		// Run initializer
		base.init();
	}

  return elements.forEach(function (element) {
    var instance = dataStore.get(element, name)
    if (instance) {
      if (typeof options === 'string') {
        instance[options].apply(instance);
} else
{
	instance.updateOptions(options);
}
    } else if (options !== 'destroy')
{
	dataStore.put(element, name, new Plugin(element, options));
}
  });
}

let elements = document.querySelectorAll('table.overview.table-fixed.stripped')
stickytheadapply(elements, { fixedOffset: document.getElementById('divHeader') });
";
		}

		private string GetGroupingCss(bool namespacedClasses)
        {
            if (namespacedClasses)
            {
				return "";
            }
			return HideGroupingCss();

		}

		private string HideGroupingCss()
        {
			return @"
coverage-info div.customizebox div:nth-child(2) { visibility:hidden;font-size:1px;height:1px;padding:0;border:0;margin:0 }
coverage-info div.customizebox div:nth-child(2) * { visibility:hidden;font-size:1px;height:1px;padding:0;border:0;margin:0 }
";
		}

		private string CoverageInfoObserver()
		{
			var code = @"
var coverageInfoObserver = (function(){
	var mutationObserver;
    var callbacks = [];
    function observe(){
	    mutationObserver.observe(  
			document.querySelector(""coverage-info""),
			{ attributes: false, childList: true, subtree: true }
		)     
    }
	function cb(record,obs){
	    mutationObserver.disconnect();
		for(var i=0;i<callbacks.length;i++){
            callbacks[i]();
        }
		observe();
	}
	return {
		observe:function(callback){
		    callbacks.push(callback);
            if(!mutationObserver){
                mutationObserver = new MutationObserver(cb);
                observe();
            }
		}
	}
})();
";
			return code;
		}
		private string ObserveAndHideFullyCovered(IAppOptions appOptions)
        {
            if (!(appOptions.HideFullyCovered | appOptions.Hide0Coverage | appOptions.Hide0Coverable))
            {
				return "";
            }
			var code = $@"
function getCellValue(row, index){{
  return parseInt(row.cells[index].innerText);
}}


var hideCoverage = function() {{
	var rows = document.querySelectorAll(""coverage-info table tbody tr"");
	for(var i=0;i<rows.length;i++){{
		var row = rows[i];
		let hide = false;
    
		const coverable = getCellValue(row,3);
		const covered = getCellValue(row,1)
	   if(coverable === 0){{
		if({appOptions.Hide0Coverable.ToString().ToLower()}){{
			hide = true;
		}}
	   }} else if(covered === 0){{
		if({appOptions.Hide0Coverage.ToString().ToLower()}){{
			hide = true;
		}}
	   }} else if(covered === coverable){{
  
		 const branchCovered = getCellValue(row,7);
		 const branchTotal = getCellValue(row,8);
    
		  if(branchTotal === branchCovered){{
			if({appOptions.HideFullyCovered.ToString().ToLower()}){{
			  hide = true;
			}}
		}}
	  }}

	  if(hide){{
		row.style.display = ""none"";
	  }}
    
	}};
}};
hideCoverage();
coverageInfoObserver.observe(hideCoverage);
";
			return code;
		}

        private string ObserveAndHideNamespaceWhenGroupingByNamespace(IAppOptions appOptions)
        {
			
			if (!appOptions.NamespacedClasses || appOptions.NamespaceQualification == NamespaceQualification.FullyQualified)
			{
				return "";
			}
            string fullyQualifiedToName;
            switch (appOptions.NamespaceQualification)
			{
                case NamespaceQualification.AlwaysUnqualified:
                case NamespaceQualification.UnqualifiedByNamespace:
					fullyQualifiedToName = "var name = fullyQualified.substring(fullyQualified.lastIndexOf(\".\") + 1);";
                    break;
                case NamespaceQualification.QualifiedByNamespaceLevel:
					fullyQualifiedToName = @"
var parts = fullyQualified.split(""."");
var namespaceParts = parts.slice(0,parts.length-1);
var type = parts[parts.length-1];
var name = type;
if(namespaceParts.length > groupingLevel){
	name = namespaceParts.slice(groupingLevel).join(""."") + ""."" + type;
}";
					break;
                default:
                    throw new Exception($"Unknown GroupingNamespaceQualification '{appOptions.NamespaceQualification}'");
            }
			var alwaysUnqualified = appOptions.NamespaceQualification == NamespaceQualification.AlwaysUnqualified;
            var code = $@"
var config = {{ attributes: false, childList: true, subtree: true }};

var changeQualification = function() {{
  var groupingInput = document.querySelector(""coverage-info .customizebox input"");
  if(!groupingInput || groupingInput.value <= 0 && !{alwaysUnqualified.ToString().ToLower()}){{
    return;
  }}

    var groupingLevel = groupingInput.value;
	var rows = document.querySelectorAll(""coverage-info table tbody tr[class-row]"");
	for(var i=0;i<rows.length;i++){{
		var row = rows[i];
        var cell = row.cells[0];
        var a = cell.querySelector(""a"");
        var fullyQualified = a.innerText;
        {fullyQualifiedToName}
        a.innerText = name;
	}};
}};
changeQualification();
coverageInfoObserver.observe(changeQualification);
";
			return code;
        }

        private string HackGroupingToAllowAll(int groupingLevel)
        {
			return $@"
				var customizeBox = document.getElementsByClassName('customizebox')[0];
				if(customizeBox){{
					var groupingInput = customizeBox.querySelector('input');
					groupingInput.max = {groupingLevel};
				}}
				
";

		}

		private string GetFontNameSize()
        {
			return $"{FontSize}{FontName}";
		}

		private string HideCyclomaticComplexityLink()
        {
			return @"
risk-hotspots > div > table > thead > tr > th:last-of-type > a:last-of-type {
		display:none
}
";

		}

		/*
			whilst hacking at the report generator html ! can use this to save to html and inspect with chrome dev tools 
		*/
		private string AddCopyDocToClipboardButtonIfRequired(bool required = false)
		{
			var code = @"
					<script>function clipboardHtml(){ window.clipboardData.setData('Text',document.documentElement.innerHTML)}</script>
					<button onclick='clipboardHtml()'>Clipboard html</button>
";
			return required ? code : "";

        }
		private string hotspotsPath;
		private string WriteHotspotsToOutputFolder(List<Hotspot> hotspots, string reportOutputFolder)
		{
			var rootElement = new XElement("Hotspots",
				hotspots.Select(hotspot =>
				{
					return new XElement("Hotspot",
						// escaping...
						new XElement("Assembly", hotspot.Assembly),
						new XElement("Class", hotspot.Class),
						new XElement("MethodName", hotspot.MethodName),
						new XElement("ShortName", hotspot.ShortName),
						new XElement("Line", hotspot.Line.HasValue ? hotspot.Line.Value.ToString() : ""),
						// do I need this
						new XElement("FileIndex", hotspot.FileIndex),
						new XElement("Metrics", hotspot.Metrics.Select(metric =>
						{
							return new XElement("Metric",
								new XElement("Name", metric.Name),
								new XElement("Exceeded", metric.Exceeded),
								new XElement("Value",metric.Value.HasValue ? metric.Value.ToString() : "") 
							);
						}).ToList()
					));
				}).ToList()
			);
			hotspotsPath = Path.Combine(reportOutputFolder, "hotspots.xml");

            rootElement.Save(hotspotsPath);
			return hotspotsPath;
			
		}

		private void RemoveHistoryChartRendering(string html,StringBuilder sb)
		{
			var start = html.IndexOf("var charts = document.getElementsByClassName('historychart');");
			var end = html.IndexOf("var assemblies = [");
			var toRemove = html.Substring(start, end - start);
			sb.Replace(toRemove, @"
var charts = document.getElementsByClassName('historychart');
for(var i=0;i<charts.length;i++){
    var chart = charts[i];
    chart.parentNode.removeChild(chart);
}
");
		}

		public string ProcessUnifiedHtml(string htmlForProcessing, string reportOutputFolder)
		{
			previousFontSizeName = GetFontNameSize();
			unprocessedReport = htmlForProcessing;
			previousReportOutputFolder = reportOutputFolder;
			var previousLogMessages = $"[{string.Join(",",logs.Select(l => $"'{l}'"))}]";
			var appOptions = appOptionsProvider.Get();
			var namespacedClasses = appOptions.NamespacedClasses;
			ReportColours = reportColoursProvider.GetColours();
			return assemblyUtil.RunInAssemblyResolvingContext(() =>
			{
				var (cyclomaticThreshold, crapScoreThreshold, nPathThreshold) = HotspotThresholds(appOptions);
				var noRiskHotspotsHeader = "No risk hotspots that exceed options :";
				var noRiskHotspotsCyclomaticMsg = $"Cyclomatic complexity : {cyclomaticThreshold}";
				var noRiskHotspotsNpathMsg =$"NPath complexity      : {nPathThreshold}";
				var noRiskHotspotsCrapMessage = $"Crap score            : {crapScoreThreshold}";
				var doc = new HtmlDocument
				{
					OptionFixNestedTags = true,
					OptionAutoCloseOnEnd = true
				};
				

				doc.LoadHtml(htmlForProcessing);
                SetInitialTheme(doc);
				htmlForProcessing = null;

				doc.DocumentNode.QuerySelectorAll(".footer").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));
				doc.DocumentNode.QuerySelectorAll(".container").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:25px;border:0"));
				doc.DocumentNode.QuerySelectorAll(".containerleft").ToList().ForEach(x => x.SetAttributeValue("style", "margin:0;padding:0;border:0"));
				doc.DocumentNode.QuerySelectorAll(".containerleft > h1 , .containerleft > p").ToList().ForEach(x => x.SetAttributeValue("style", "display:none"));

				// DOM changes

				HideRowsFromOverviewTable(doc);
				

				// TEXT changes
				var assemblyClassDelimiter = "!";

				var outerHtml = doc.DocumentNode.OuterHtml;
				var htmlSb = new StringBuilder(outerHtml);
				FixGroupingMax(htmlSb);
				FixCollapse(htmlSb);
				RemoveHistoryChartRendering(outerHtml,htmlSb);
                var assembliesSearch = "var assemblies = [";
				var startIndex = outerHtml.IndexOf(assembliesSearch) + assembliesSearch.Length - 1;
				var endIndex = outerHtml.IndexOf("var historicCoverageExecutionTimes");
				var assembliesToReplace = outerHtml.Substring(startIndex, endIndex - startIndex);
				endIndex = assembliesToReplace.LastIndexOf(']');
				assembliesToReplace = assembliesToReplace.Substring(0, endIndex + 1);

				var assemblies = JArray.Parse(assembliesToReplace);
				var groupingLevel = 0;
				foreach (var assembly in assemblies.Cast<JObject>())
				{
					var assemblyName = assembly["name"];
					var classes = assembly["classes"] as JArray;

					var autoGeneratedRemovals = new List<JObject>();
					foreach (var @class in classes.Cast<JObject>())
					{
						var className = @class["name"].ToString();
						if (className == "AutoGeneratedProgram")
						{
							autoGeneratedRemovals.Add(@class);
						}
						else
						{
							var namespaces = className.Split('.').Length - 1;
							if (namespaces > groupingLevel)
							{
								groupingLevel = namespaces;
							}
							if (!namespacedClasses)
							{
								// simplify name
								var lastIndexOfDotInName = className.LastIndexOf('.');
								if (lastIndexOfDotInName != -1) @class["name"] = className.Substring(lastIndexOfDotInName).Trim('.');
							}
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
				foreach (var riskHotspot in riskHotspots.Cast<JObject>())
				{
					var assembly = riskHotspot["assembly"].ToString();
					var qualifiedClassName = riskHotspot["class"].ToString();
					if (!namespacedClasses)
					{
						// simplify name
						var lastIndexOfDotInName = qualifiedClassName.LastIndexOf('.');
						if (lastIndexOfDotInName != -1) riskHotspot["class"] = qualifiedClassName.Substring(lastIndexOfDotInName).Trim('.');
					}
					var newReportPath = $"#{assembly}{assemblyClassDelimiter}{qualifiedClassName}.html";
					riskHotspot["reportPath"] = newReportPath;
				}
				var riskHotspotsReplaced = riskHotspots.ToString();
				htmlSb.Replace(rhToReplace, riskHotspotsReplaced);

				htmlSb.Replace(".table-fixed", ".table-fixed-ignore-me");

				var fontColour = jsReportColours.FontColour;
				var scrollbarThumbColour = jsReportColours.ScrollBarThumbColour;
				var sliderThumbColour = jsReportColours.SliderThumbColour;
				htmlSb.Replace("</head>", $@"
				<style id=""fccStyle1"" type=""text/css"">
					*, body {{ font-family:{FontName};font-size: {FontSize}; color: {fontColour}}}
					button {{ cursor:pointer; padding:10px; color: {jsReportColours.ButtonTextColour}; background:{jsReportColours.ButtonColour}; border-color:{jsReportColours.ButtonBorderColour}}}
					button:hover {{ color : {jsReportColours.ButtonHoverTextColour}; background:{jsReportColours.ButtonHoverColour}; border-color:{jsReportColours.ButtonBorderHoverColour}}}
					button:active {{ color : {jsReportColours.ButtonPressedTextColour}; background:{jsReportColours.ButtonPressedColour}; border-color:{jsReportColours.ButtonBorderPressedColour}}}
					table td {{ white-space: nowrap; }}
					body {{ padding-left:3px;padding-right:3px;padding-bottom:3px }}
					{HideCyclomaticComplexityLink()}
					body {{ -webkit-user-select:none;-moz-user-select:none;-ms-user-select:none;-o-user-select:none;user-select:none }}
					table.overview th, table.overview td {{ white-space: nowrap; word-break: normal; padding-left:10px;padding-right:10px; }}
					table.coverage {{ width:150px;height:13px;margin-left:10px;margin-right:10px }}
					table.coverage th, table.coverage td {{ padding-left:0px;padding-right:0px}}
					{GetGroupingCss(namespacedClasses)}
					table,tr,th,td {{ border: 1px solid;}}
					input[type=text] {{ color:{jsReportColours.TextBoxTextColour}; background-color:{jsReportColours.TextBoxColour};border-color:{jsReportColours.TextBoxBorderColour} }}
					select {{ color:{jsReportColours.ComboBoxTextColour}; background-color:{jsReportColours.ComboBoxColour};border-color:{jsReportColours.ComboBoxBorderColour} }}
                    body, html {{ scrollbar-arrow-color:{jsReportColours.ScrollBarArrowColour};scrollbar-track-color:{jsReportColours.ScrollBarTrackColour};scrollbar-face-color:{scrollbarThumbColour};scrollbar-shadow-color:{scrollbarThumbColour};scrollbar-highlight-color:{scrollbarThumbColour};scrollbar-3dlight-color:{scrollbarThumbColour};scrollbar-darkshadow-color:{scrollbarThumbColour} }}				
					input[type=range]::-ms-thumb {{
					  background: {sliderThumbColour};
					  border: {sliderThumbColour}
					}}
					input[type=range]::-ms-track {{
						color: transparent;
						border-color: transparent;
						background: transparent;
					}}
					
					input[type=range]::-ms-fill-lower {{
					  background: {jsReportColours.SliderLeftColour};  
					}}
					input[type=range]::-ms-fill-upper {{
					  background: {jsReportColours.SliderRightColour}; 
					}}
				</style>
				</head>
			");

				htmlSb.Replace("</body>", $@"
					<script type=""text/javascript"">
						{GetStickyTableHead(appOptions)}
						{HackGroupingToAllowAll(groupingLevel)}
                        {CoverageInfoObserver()}
						{ObserveAndHideNamespaceWhenGroupingByNamespace(appOptions)}
						{ObserveAndHideFullyCovered(appOptions)}
						function getRuleBySelector(cssRules,selector){{
						for(var i=0;i<cssRules.length;i++){{
							if(cssRules[i].selectorText == selector){{
								return cssRules[i];
							}}
						}}
					}}
					function getStyleBySelector(cssRules,selector){{
						return getRuleBySelector(cssRules,selector).style;
					}}

					function getStyleSheetById(id){{
						for(var i=0;i<document.styleSheets.length;i++){{
							var styleSheet = document.styleSheets[i];
							if(styleSheet.ownerNode && styleSheet.ownerNode.id == id){{
								return styleSheet;
							}}
						}}
					}}
					function {FontChangedJSFunctionName}(fontNameAndSize){{
						var parts = fontNameAndSize.split(':');
						var fccStyleSheet1Rules = getStyleSheetById('fccStyle1').cssRules;
						var generalStyle = getStyleBySelector(fccStyleSheet1Rules,'*, body');
						generalStyle.setProperty('font-family',parts[0]);
						generalStyle.setProperty('font-size',parts[1]);
					}}
					
						var htmlExtension = '.html';
						
						var eventListener = function (element, event, func) {{
							if (element.addEventListener)
								element.addEventListener(event, func, false);
							else if (element.attachEvent)
								element.attachEvent('on' + event, func);
							else
								element['on' + event] = func;
						}};

						eventListener(window,'focus',function(){{window.external.{nameof(ScriptManager.DocumentFocused)}}});

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
				<style type=""text/css"" id='fccMediaStyle'>
					@media screen and (-ms-high-contrast:active){{
						table.coverage > td.green{{ background-color: windowText }}
						table.coverage > td.gray{{ 
							background-color: {jsReportColours.GrayCoverageColour}
						}}
	
					}}
				</style>

				</head>
			");

				htmlSb.Replace("<body>", $@"
					<body oncontextmenu='return false;'>
					<style id='fccStyle2'>
						#divHeader {{
							background-color: {jsReportColours.DivHeaderBackgroundColour};
						}}
						table#headerTabs td {{
							color: {jsReportColours.HeaderFontColour};
							border-color: {jsReportColours.HeaderBorderColour};
						}}	
						table#headerTabs td {{
							border-width:3px;
							padding: 3px;
							padding-left: 7px;
							padding-right: 7px;
						}}
						table#headerTabs td.tab {{
							cursor: pointer;
							background-color : {jsReportColours.TabBackgroundColour};
						}}
						table#headerTabs td.active {{
							border-bottom: 3px solid transparent;
							font-weight: bolder;
						}}
					
					</style>
					<script>
						var body = document.getElementsByTagName('body')[0];
						body.style['padding-top'] = '50px';
					
						var tabs = [
							{{ button: 'btnCoverage', content: 'coverage-info' }}, 
							{{ button: 'btnSummary', content: 'table-fixed' }},
							{{ button: 'btnRiskHotspots', content: 'risk-hotspots' }},
							{{ button: 'btnCoverageLog', content: 'coverage-log' }},
						];

						var riskHotspotsTable;
						var riskHotspotsElement;
						var addedFileIndexToRiskHotspots = false;
						var addFileIndexToRiskHotspotsClassLink = function(){{
						  if(!addedFileIndexToRiskHotspots){{
							addedFileIndexToRiskHotspots = true;
							var riskHotspotsElements = document.getElementsByTagName('risk-hotspots');
							if(riskHotspotsElements.length == 1){{
								riskHotspotsElement = riskHotspotsElements[0];
								riskHotspotsTable = riskHotspotsElement.querySelector('table');
								if(riskHotspotsTable){{
									var rhBody = riskHotspotsTable.querySelector('tbody');
									var rows = rhBody.rows;
									for(var i=0;i<rows.length;i++){{
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
									}}
								}}
								
							}}
							}}
						}}
						
						// necessary for WebBrowser 
						function removeElement(element){{
							element.parentNode.removeChild(element);
						}}

						function insertAfter(newNode, existingNode) {{
							existingNode.parentNode.insertBefore(newNode, existingNode.nextSibling);
						}}

						var noHotspotsMessage
						var addNoRiskHotspotsMessageIfRequired = function(){{
							if(riskHotspotsTable == null){{
								noHotspotsMessage = document.createElement(""p"");
								noHotspotsMessage.style.margin = ""0"";
								var header = ""{noRiskHotspotsHeader}"";
								var cyclomaticMessage = ""{noRiskHotspotsCyclomaticMsg}"";
								var crapMessage =""{noRiskHotspotsCrapMessage}""; 
								var nPathMessage = ""{noRiskHotspotsNpathMsg}"";
								noHotspotsMessage.innerText = header + ""\n"" + cyclomaticMessage + ""\n"" + crapMessage + ""\n"" + nPathMessage;

								insertAfter(noHotspotsMessage, riskHotspotsElement);
							}}
						}}

						var removeNoRiskHotspotsMessage = function(){{
							if(noHotspotsMessage){{
								removeElement(noHotspotsMessage);
								noHotspotsMessage = null;
							}}
						}}

						var openTab = function (tabIndex) {{
							if(tabIndex==2){{
								addFileIndexToRiskHotspotsClassLink();
								addNoRiskHotspotsMessageIfRequired();
							}}else{{
								removeNoRiskHotspotsMessage();
							}}
							for (var i = 0; i < tabs.length; i++) {{
							
								var tab = tabs[i];
								if (!tab) continue;
							
								var button = document.getElementById(tab.button);
								if (!button) continue;
							
								var content = document.getElementsByTagName(tab.content)[0];
								if (!content) content = document.getElementsByClassName(tab.content)[0];
								if (!content) continue;
							
								if (i == tabIndex) {{
									if (button.className.indexOf('active') == -1) button.className += ' active';
									content.style.display = 'block';
								}} else {{
									button.className = button.className.replace('active', '');
									content.style.display = 'none';
								}}
							}}
						}};
					
						window.addEventListener('load', function() {{
							addCoverageLogElements();
							openTab(0);
						}});

						var previousLogMessages = {previousLogMessages}
						var coverageLogElement;

						function clearFCCWindowLogs(){{
							coverageLogElement.textContent = '';
							window.external.{nameof(ScriptManager.ClearFCCWindowLogs)}();
						}}

						function addCoverageLogElements(){{
							var container = document.getElementsByClassName('container')[0];
							var coverageLogContainer = document.createElement('div');
							
							coverageLogContainer.className = 'coverage-log';
							var clearFCCWindowLogsButton = document.createElement('button');
							clearFCCWindowLogsButton.textContent = 'Clear';
							clearFCCWindowLogsButton.onclick = clearFCCWindowLogs;
							coverageLogContainer.appendChild(clearFCCWindowLogsButton);
                            coverageLogElement = document.createElement('div');
							coverageLogElement.style.marginTop = '25px';
							for(var i =0 ; i< previousLogMessages.length;i++){{
								addLogMessageElement(previousLogMessages[i]);
							}}
							coverageLogContainer.appendChild(coverageLogElement);
							container.appendChild(coverageLogContainer);
						}}
						function addExternalMessage(logElement,message,matchLinkPart, externalFn){{
							var matched = false;
							var startIndex = message.indexOf(matchLinkPart);
							if(startIndex != -1){{
								matched = true;
								if(startIndex != 0){{
									var before = message.substring(0,startIndex);
									var beforeEl = document.createElement('span');
									beforeEl.innerText = before;
									logElement.appendChild(beforeEl);
								}}
								var externalLink = document.createElement('a');
								externalLink.innerText = matchLinkPart;
								externalLink.href = '#';
								externalLink.onclick = function(){{
									window.external[externalFn]();
									return false; 
								}}
								logElement.appendChild(externalLink);
								var after = message.substring(startIndex + matchLinkPart.length);
								if(after != ''){{
									var afterEl = document.createElement('span');
									afterEl.innerText = after;
									logElement.appendChild(afterEl);
								}}
							}}
							return matched;
						}}
						function addLogMessageElement(message){{
							var logElement = document.createElement('div');
							var matched = addExternalMessage(logElement,message,'FCC Output Pane','{nameof(ScriptManager.ShowFCCOutputPane)}');
							if(!matched){{
								matched = addExternalMessage(logElement,message,'View readme','{nameof(ScriptManager.ReadReadMe)}');
							}}
							if(!matched){{
								logElement.innerText = message;
							}}
							
							coverageLogElement.insertBefore(logElement, coverageLogElement.firstChild);
						}}
						function {ShowFCCWorkingJSFunctionName}(isWorking){{
							var coverageLogTab = document.getElementById('btnCoverageLog');
							coverageLogTab.innerText = isWorking ? '{CoverageLogTabName} *' : '{CoverageLogTabName}';
						}}

						function {CoverageLogJSFunctionName}(message){{
							showFCCWorking(true);
							addLogMessageElement(message);
						}}
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
								<td id='btnCoverageLog' onclick='return openTab(3);' class='tab' style='width:1%;white-space:no-wrap'>
									{CoverageLogTabName}
								</td>
								<td style='border-top:transparent;border-right:transparent;padding-top:0px' align='center'>
									<a href='#' onclick='return window.external.{nameof(ScriptManager.RateAndReview)}();' style='margin-right:7px'>Rate & Review</a>
									<a href='#' onclick='return window.external.{nameof(ScriptManager.LogIssueOrSuggestion)}();' style='margin-left:7px'>Log Issue/Suggestion</a>
								</td>
								<td style='width:1%;white-space:no-wrap;border-top:transparent;border-right:transparent;border-left:transparent;padding-top:0px'>
									<a href='#' onclick='return window.external.{nameof(ScriptManager.BuyMeACoffee)}();'>Buy me a coffee</a>
								</td>
							</tr>
						</table>
					</div>
					{AddCopyDocToClipboardButtonIfRequired()}
				");

                if (!showBranchCoverage)
                {
					htmlSb.Replace("branchCoverageAvailable = true", "branchCoverageAvailable = false");
				}
				
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

				if (reportOutputFolder != null)
				{
					var processedHtmlFile = Path.Combine(reportOutputFolder, "index-processed.html");
					File.WriteAllText(processedHtmlFile, processed);
				}

				return processed;

			});
		}

		private void PreventBrowserHistory(StringBuilder documentStringBuilder)
        {
			documentStringBuilder.Replace(
				@"{key:""onDonBeforeUnlodad"",value:function(){if(this.saveCollapseState(),void 0!==this.window.history&&void 0!==this.window.history.replaceState){console.log(""Coverage info: Updating history"",this.settings);var e=null;(e=null!==window.history.state?JSON.parse(JSON.stringify(this.window.history.state)):new Gc).coverageInfoSettings=JSON.parse(JSON.stringify(this.settings)),window.history.replaceState(e,null)}}},",
				@"{key:""onDonBeforeUnlodad"",value: function(){}},");
        }

		private void FixCollapse(StringBuilder documentStringBuilder)
        {
			documentStringBuilder.Replace(
				@"{key:""saveCollapseState"",value:function(){var e=this;this.settings.collapseStates=[],function t(n){for(var r=0;r<n.length;r++)e.settings.collapseStates.push(n[r].collapsed),t(n[r].subElements)}(this.codeElements)}},{key:""restoreCollapseState"",value:function(){var e=this,t=0;!function n(r){for(var i=0;i<r.length;i++)e.settings.collapseStates.length>t&&(r[i].collapsed=e.settings.collapseStates[t]),t++,n(r[i].subElements)}(this.codeElements)}}",
				@"{
					key:""saveCollapseState"",
					value:function(){
						var e=this;
						this.settings.collapseStates=[];
						function t(level,n){
							for(var r=0;r<n.length;r++){
								console.log(n[r].name);
								
								e.settings.collapseStates.push(n[r].name + ':' + level.toString() + ':' + n[r].collapsed.toString())
								t(level+1,n[r].subElements)
							}
						}
						t(0,this.codeElements);
					}
				},{
					key:""restoreCollapseState"",
					value:function(){
						var e=this;
						var collapsedStates = e.settings.collapseStates;
						function n(level,r){
							for(var i=0;i<r.length;i++){
								var codeElement = r[i];
								for(var j=0;j<collapsedStates.length;j++){
									var collapsedState = collapsedStates[j];
									var parts = collapsedState.split(':');
									var name = parts[0];
									var stateLevel = parts[1];
									var isCollapsed = (parts[2] === 'true');
									if(name == codeElement.name && stateLevel == level.toString()){
										codeElement.collapsed = isCollapsed;
										break;
									}
								}
								n(level + 1, r[i].subElements);// conditional on collapsed ?
								
							}
						}
						n(0,this.codeElements);
					}
				}");
        }

		private void FixGroupingMax(StringBuilder documentStringBuilder)
        {
			documentStringBuilder.Replace(
				@"{key:""ngOnInit"",value:function(){this.historicCoverageExecutionTimes=this.window.historicCoverageExecutionTimes,this.branchCoverageAvailable=this.window.branchCoverageAvailable,this.translations=this.window.translations;var e=!1;if(void 0!==this.window.history&&void 0!==this.window.history.replaceState&&null!==this.window.history.state&&null!=this.window.history.state.coverageInfoSettings)console.log(""Coverage info: Restoring from history"",this.window.history.state.coverageInfoSettings),e=!0,this.settings=JSON.parse(JSON.stringify(this.window.history.state.coverageInfoSettings));else{for(var t=0,n=this.window.assemblies,r=0;r<n.length;r++)for(var i=0;i<n[r].classes.length;i++)t=Math.max(t,(n[r].classes[i].name.match(/\./g)||[]).length);this.settings.groupingMaximum=t,console.log(""Grouping maximum: ""+t)}var o=window.location.href.indexOf(""?"");o>-1&&(this.queryString=window.location.href.substr(o)),this.updateCoverageInfo(),e&&this.restoreCollapseState()}}",
				@"{key:""ngOnInit"",value:function(){
					this.historicCoverageExecutionTimes=this.window.historicCoverageExecutionTimes;
                    this.branchCoverageAvailable=this.window.branchCoverageAvailable;
                    this.translations=this.window.translations;
                    var restoredFromHistory = false;
				    if(void 0!==this.window.history&&void 0!==this.window.history.replaceState&&null!==this.window.history.state&&null!=this.window.history.state.coverageInfoSettings){
						restoredFromHistory = true;
						this.settings=JSON.parse(JSON.stringify(this.window.history.state.coverageInfoSettings));
						
					}

					for(var t=0,n=this.window.assemblies,r=0;r<n.length;r++){
						for(var i=0;i<n[r].classes.length;i++){
							t=Math.max(t,(n[r].classes[i].name.match(/\./g)||[]).length);
						}
					}
					this.settings.groupingMaximum=t;
					if(this.settings.grouping > this.settings.groupingMaximum){
						this.settings.grouping = this.settings.groupingMaximum;
					}
					
					this.updateCoverageInfo();
					if(restoredFromHistory){
						this.restoreCollapseState()
					}
					
				}}");

        }

		private void HideRowsFromOverviewTable(HtmlDocument doc)
        {
			var table = doc.DocumentNode.QuerySelectorAll("table.overview").First();
			var tableRows = table.QuerySelectorAll("tr").ToArray();
			try { tableRows[0].SetAttributeValue("style", "display:none"); } catch { } // generated on
			try { tableRows[1].SetAttributeValue("style", "display:none"); } catch { } // parser
			if (!showBranchCoverage)
			{
				try { tableRows[10].SetAttributeValue("style", "display:none"); } catch { } // covered branches
				try { tableRows[11].SetAttributeValue("style", "display:none"); } catch { } // total branches
				try { tableRows[12].SetAttributeValue("style", "display:none"); } catch { } // branch coverage
			}
		}

		private (int cyclomaticThreshold, int crapScoreThreshold, int nPathThreshold) HotspotThresholds(IAppOptions appOptions)
        {
			return (
				appOptions.ThresholdForCyclomaticComplexity,
				appOptions.ThresholdForCrapScore,
				appOptions.ThresholdForNPathComplexity
			);

		}

		private void ReportColoursProvider_ColoursChanged(object sender, IReportColours reportColours)
		{
			ReprocessReport();
		}

        public string BlankReport(bool withHistory)
        {
            if (!withHistory)
            {
				logs.Clear();
            }
			return ProcessUnifiedHtml(GetDummyReportToProcess(),null);
        }

		private string GetDummyReportToProcess()
		{
			return resourceProvider.ReadResource("dummyReportToProcess.html");
        }

        public void LogCoverageProcess(string message)
        {
			message = $"{NowForLog.Get()} : {message}";
            eventAggregator.SendMessage(new InvokeScriptMessage(CoverageLogJSFunctionName, message));
			logs.Add(message);
		}

        public void EndOfCoverageRun()
        {
			eventAggregator.SendMessage(new InvokeScriptMessage(ShowFCCWorkingJSFunctionName, false));
		}
		
		public void UpdateReportWithDpiFontChanges()
        {
			if (unprocessedReport !=null && previousFontSizeName != GetFontNameSize())
			{
				ReprocessReport();
			}
        }

		private void ReprocessReport()
        {
			if(unprocessedReport == null)
			{
				unprocessedReport = GetDummyReportToProcess();
			}
			var newReport = ProcessUnifiedHtml(unprocessedReport, previousReportOutputFolder);
			eventAggregator.SendMessage(new NewReportMessage { Report = newReport });
		}

		public void Handle(EnvironmentFontDetailsChangedMessage message)
		{
			environmentFontDetails = message.FontDetails;
			UpdateReportWithDpiFontChanges();
		}

		public void Handle(DpiChangedMessage message)
		{
			dpiScale = message.DpiScale;
			UpdateReportWithDpiFontChanges();
		}

		public void Handle(ReadyForReportMessage message)
		{
			var newReport = BlankReport(false);
			eventAggregator.SendMessage(new ObjectForScriptingMessage { ObjectForScripting = scriptManager });
			eventAggregator.SendMessage(new NewReportMessage { Report = newReport });
		}
	}
}
