using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine
{
    internal enum ReloadCoverageStatus { Start, Done, Cancelled, Error, Initializing };

    [Export(typeof(IFCCEngine))]
    internal class FCCEngine : IFCCEngine
    {
        internal int InitializeWait { get; set; } = 5000;
        internal const string initializationFailedMessagePrefix = "Initialization failed.  Please check the following error which may be resolved by reopening visual studio which will start the initialization process again.";
        private readonly object colorThemeService;        
        private CancellationTokenSource cancellationTokenSource;

        public event UpdateMarginTagsDelegate UpdateMarginTags;
        public event UpdateOutputWindowDelegate UpdateOutputWindow;

        public string AppDataFolderPath { get; private set; }
        public List<CoverageLine> CoverageLines { get; internal set; }
        public string SolutionPath { get; set; }


        private readonly ICoverageUtilManager coverageUtilManager;
        private readonly ICoberturaUtil coberturaUtil;        
        private readonly IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly IProcessUtil processUtil;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;
        private readonly IServiceProvider serviceProvider;

        private IInitializeStatusProvider initializeStatusProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        internal System.Threading.Tasks.Task reloadCoverageTask;

        [ImportingConstructor]
        public FCCEngine(
            ICoverageUtilManager coverageUtilManager,
            ICoberturaUtil coberturaUtil,
            IMsTestPlatformUtil msTestPlatformUtil,            
            IReportGeneratorUtil reportGeneratorUtil,
            IProcessUtil processUtil,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger,
            IAppDataFolder appDataFolder,
            ICoverageToolOutputManager coverageOutputManager,
            IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService,
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
            this.coverageOutputManager = coverageOutputManager;
            this.coverageUtilManager = coverageUtilManager;
            this.coberturaUtil = coberturaUtil;
            this.msTestPlatformUtil = msTestPlatformUtil;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.processUtil = processUtil;
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.appDataFolder = appDataFolder;
            this.serviceProvider = serviceProvider;            
            this.msCodeCoverageRunSettingsService = msCodeCoverageRunSettingsService;
            colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));
        }

        internal string GetLogReloadCoverageStatusMessage(ReloadCoverageStatus reloadCoverageStatus)
        {
            return $"================================== {reloadCoverageStatus.ToString().ToUpper()} ==================================";
        }
        private void LogReloadCoverageStatus(ReloadCoverageStatus reloadCoverageStatus)
        {
            logger.Log(GetLogReloadCoverageStatusMessage(reloadCoverageStatus));
        }

        public void Initialize(IInitializeStatusProvider initializeStatusProvider)
        {
            this.initializeStatusProvider = initializeStatusProvider;

            appDataFolder.Initialize();
            AppDataFolderPath = appDataFolder.DirectoryPath;

            reportGeneratorUtil.Initialize(AppDataFolderPath);
            msTestPlatformUtil.Initialize(AppDataFolderPath);
            coverageUtilManager.Initialize(AppDataFolderPath);
            msCodeCoverageRunSettingsService.Initialize(AppDataFolderPath);
        }

        public void ClearUI()
        {
            CoverageLines = null;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());

            UpdateOutputWindow?.Invoke(new UpdateOutputWindowEventArgs { });
        }

        public void StopCoverage()
        {           
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private CancellationToken Reset()
        {
            ClearUI();
            StopCoverage();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            processUtil.CancellationToken = cancellationToken;

            return cancellationToken;
        }

        private async System.Threading.Tasks.Task<string[]> RunCoverageAsync(List<ICoverageProject> coverageProjects,CancellationToken cancellationToken)
        {
            // process pipeline

            await PrepareCoverageProjectsAsync(coverageProjects, cancellationToken);

            foreach (var coverageProject in coverageProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await coverageProject.StepAsync("Run Coverage Tool", (project) => coverageUtilManager.RunCoverageAsync(project, true));
            }

            var passedProjects = coverageProjects.Where(p => !p.HasFailed);

            return passedProjects
                    .Select(x => x.CoverageOutputFile)
                    .ToArray();

        }

        private void RaiseUpdateOutputWindow(string reportHtml)
        {
            UpdateOutputWindowEventArgs updateOutputWindowEventArgs = new UpdateOutputWindowEventArgs { HtmlContent = reportHtml};
            UpdateOutputWindow?.Invoke(updateOutputWindowEventArgs);
        }
        private void UpdateUI(List<CoverageLine> coverageLines, string reportHtml)
        {
            CoverageLines = coverageLines;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());
            RaiseUpdateOutputWindow(reportHtml);            
        }

        private async System.Threading.Tasks.Task<(List<CoverageLine> coverageLines,string reportFilePath)> RunAndProcessReportAsync(string[] coverOutputFiles, string reportOutputFolder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<CoverageLine> coverageLines = null;
            string processedReport = null;

            var result = await reportGeneratorUtil.GenerateAsync(coverOutputFiles,reportOutputFolder, true);

            if (result.Success)
            {
                logger.Log("Processing cobertura");
                coberturaUtil.ProcessCoberturaXml(result.UnifiedXmlFile);
                coverageLines = coberturaUtil.CoverageLines;

                logger.Log("Processing report");
                processedReport = reportGeneratorUtil.ProcessUnifiedHtml(result.UnifiedHtml,reportOutputFolder);
            }
            return (coverageLines, processedReport);
        }

        private async System.Threading.Tasks.Task PrepareCoverageProjectsAsync(List<ICoverageProject> coverageProjects, CancellationToken cancellationToken)
        {
            foreach (var project in coverageProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(project.ProjectFile))
                {
                    project.FailureDescription = $"Unsupported project type for DLL '{project.TestDllFile}'";
                    continue;
                }

                if (!project.Settings.Enabled)
                {
                    project.FailureDescription = $"Disabled";
                    continue;
                }

                await project.PrepareForCoverageAsync();
            }
        }

        private void ReloadCoverageTaskContinuation(System.Threading.Tasks.Task<(List<CoverageLine> coverageLines, string reportHtml)> t)
        {
            switch (t.Status)
            {
                case System.Threading.Tasks.TaskStatus.Canceled:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
                    break;
                case System.Threading.Tasks.TaskStatus.Faulted:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Error);
                    logger.Log(t.Exception.InnerExceptions[0]);
                    ClearUI();
                    break;
                case System.Threading.Tasks.TaskStatus.RanToCompletion:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Done);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    UpdateUI(t.Result.coverageLines, t.Result.reportHtml);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                    break;
            }

        }

        private async System.Threading.Tasks.Task PollInitializedStatusAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var InitializeStatus = initializeStatusProvider.InitializeStatus;
                switch (InitializeStatus)
                {
                    case InitializeStatus.Initialized:
                        return;

                    case InitializeStatus.Initializing:
                        LogReloadCoverageStatus(ReloadCoverageStatus.Initializing);
                        await System.Threading.Tasks.Task.Delay(InitializeWait);
                        break;
                    case InitializeStatus.Error:
                        throw new Exception(initializationFailedMessagePrefix + Environment.NewLine + initializeStatusProvider.InitializeExceptionMessage);
                }
            }
        }

        public void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback)
        {
            var cancellationToken = Reset();
            
                reloadCoverageTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    List<CoverageLine> coverageLines = null;
                    string reportHtml = null;

                    await PollInitializedStatusAsync(cancellationToken);

                    LogReloadCoverageStatus(ReloadCoverageStatus.Start);

                    var coverageProjects = await coverageRequestCallback();
                    coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjects);                    
                    var reportOutputFolder = coverageOutputManager.GetReportOutputFolder();                    
                    var settings = appOptionsProvider.Get();
                    if (!settings.MsCodeCoverage)
                    {
                        var coverOutputFiles = await RunCoverageAsync(coverageProjects, cancellationToken);
                        if (coverOutputFiles.Any())
                        {
                            (coverageLines, reportHtml) = await RunAndProcessReportAsync(coverOutputFiles, reportOutputFolder, cancellationToken);
                        }
                    }
                    else
                    {
                        await PrepareCoverageProjectsAsync(coverageProjects, cancellationToken);
                        var outputFiles = msCodeCoverageRunSettingsService.GetCoverageFilesFromLastRun();
                        logger.Log("Number of outputfiles:" + outputFiles.Count);
                        if (outputFiles.Any())
                        {
                            (coverageLines, reportHtml) = await RunAndProcessReportAsync(outputFiles.ToArray(), reportOutputFolder, cancellationToken);
                        }
                    }
                    return (coverageLines, reportHtml);

                }, cancellationToken)
                .ContinueWith(t =>
                {
                    ReloadCoverageTaskContinuation(t);

                }, System.Threading.Tasks.TaskScheduler.Default);            
        }

        public void PrepareTestRun(ITestOperation testOperation)
        {
            msCodeCoverageRunSettingsService.PrepareRunSettings(SolutionPath, testOperation);
        }

    }
}