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
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Engine
{
    internal enum ReloadCoverageStatus { Start, Done, Cancelled, Error, Initializing };

    [Export(typeof(IFCCEngine))]
    internal class FCCEngine : IFCCEngine
    {
        internal int InitializeWait { get; set; } = 5000;
        internal const string initializationFailedMessagePrefix = "Initialization failed.  Please check the following error which may be resolved by reopening visual studio which will start the initialization process again.";
        private CancellationTokenSource cancellationTokenSource;
        
        public event UpdateMarginTagsDelegate UpdateMarginTags;
        
        public string AppDataFolderPath { get; private set; }
        public List<CoverageLine> CoverageLines { get; internal set; }

        private readonly ICoverageUtilManager coverageUtilManager;
        private readonly ICoberturaUtil coberturaUtil;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly IProcessUtil processUtil;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;
        
        private IInitializeStatusProvider initializeStatusProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        internal System.Threading.Tasks.Task reloadCoverageTask;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ISolutionEvents solutionEvents; // keep alive
#pragma warning restore IDE0052 // Remove unread private members
        private readonly IEventAggregator eventAggregator;

        [ImportingConstructor]
        public FCCEngine(
            ICoverageUtilManager coverageUtilManager,
            ICoberturaUtil coberturaUtil,
            IMsTestPlatformUtil msTestPlatformUtil,
            IReportGeneratorUtil reportGeneratorUtil,
            IProcessUtil processUtil,
            ILogger logger,
            IAppDataFolder appDataFolder,
            ICoverageToolOutputManager coverageOutputManager,
            ISolutionEvents solutionEvents,
            IAppOptionsProvider appOptionsProvider,
            IEventAggregator eventAggregator
            )
        {
            this.solutionEvents = solutionEvents;
            this.eventAggregator = eventAggregator;
            solutionEvents.AfterClosing += (s,args) => ClearOutputWindow(false);
            appOptionsProvider.OptionsChanged += (appOptions) =>
            {
                if (!appOptions.Enabled)
                {
                    ClearUI();
                }
            };
            this.coverageOutputManager = coverageOutputManager;
            this.coverageUtilManager = coverageUtilManager;
            this.coberturaUtil = coberturaUtil;
            this.msTestPlatformUtil = msTestPlatformUtil;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.processUtil = processUtil;
            this.logger = logger;
            this.appDataFolder = appDataFolder;
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
        }

        public void ClearUI()
        {
            CoverageLines = null;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());
            ClearOutputWindow(true);
        }

        private void ClearOutputWindow(bool withHistory)
        {
            RaiseUpdateOutputWindow(reportGeneratorUtil.BlankReport(withHistory));
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
            CoverageLines = null;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());

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
                var coverageProjectHasFailed = coverageProject.HasFailed;
                if (coverageProjectHasFailed) // todo remove step
                {
                    await reportGeneratorUtil.LogCoverageProcessAsync($"{coverageProject.ProjectName} failed : {coverageProject.FailureDescription}");
                }
                await coverageProject.StepAsync("Run Coverage Tool", async (project) =>
                {
                    var start = DateTime.Now;
                    try
                    {
                        var coverageTool = coverageUtilManager.CoverageToolName(project);
                        await reportGeneratorUtil.LogCoverageProcessAsync($"Starting {coverageTool} coverage for {project.ProjectName}");
                        await coverageUtilManager.RunCoverageAsync(project, true);
                    }catch(Exception exc)
                    {
                        await reportGeneratorUtil.LogCoverageProcessAsync($"{coverageProject.ProjectName} failed : {exc}");
                        throw exc;
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var duration = DateTime.Now - start;
                        var durationMessage = $"Completed coverage for {coverageProject.ProjectName} : {duration}";
                        logger.Log(durationMessage);
                        await reportGeneratorUtil.LogCoverageProcessAsync(durationMessage);
                    }
                });
            }

            var passedProjects = coverageProjects.Where(p => !p.HasFailed);

            return passedProjects
                    .Select(x => x.CoverageOutputFile)
                    .ToArray();

        }

        private void RaiseUpdateOutputWindow(string reportHtml)
        {
            eventAggregator.SendMessage(new NewReportMessage { Report = reportHtml });
        }

        private void UpdateUI(List<CoverageLine> coverageLines, string reportHtml)
        {
            CoverageLines = coverageLines;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());
            if (reportHtml == null)
            {
                reportHtml = reportGeneratorUtil.BlankReport(true);
            }
            RaiseUpdateOutputWindow(reportHtml);
        }

        private async System.Threading.Tasks.Task<(List<CoverageLine> coverageLines,string reportFilePath)> RunAndProcessReportAsync(string[] coverOutputFiles,string reportOutputFolder,CancellationToken cancellationToken)
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

                var fileSynchronizationDetails = await project.PrepareForCoverageAsync();
                var logs = fileSynchronizationDetails.Logs;
                if (logs.Any())
                {
                    foreach(var log in logs)
                    {
                        logger.Log(log);
                    }
                    logger.Log($"File synchronization duration : {fileSynchronizationDetails.Duration}");
                    var itemOrItems = logs.Count == 1 ? "item" : "items";
                    await reportGeneratorUtil.LogCoverageProcessAsync($"File synchronization {logs.Count} {itemOrItems}, duration : {fileSynchronizationDetails.Duration}");
                }
            }
        }

        private async System.Threading.Tasks.Task DisplayCoverageResultAsync(System.Threading.Tasks.Task<(List<CoverageLine> coverageLines, string reportHtml)> t)
        {
            switch (t.Status)
            {
                case System.Threading.Tasks.TaskStatus.Canceled:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
                    await reportGeneratorUtil.LogCoverageProcessAsync("Coverage cancelled");
                    break;
                case System.Threading.Tasks.TaskStatus.Faulted:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Error);
                    logger.Log(t.Exception.InnerExceptions[0]);
                    await reportGeneratorUtil.LogCoverageProcessAsync(t.Exception.ToString());
                    break;
                case System.Threading.Tasks.TaskStatus.RanToCompletion:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Done);
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                    UpdateUI(t.Result.coverageLines, t.Result.reportHtml);
#pragma warning restore VSTHRD103 // Call async methods when in an async method
                    break;
            }
            await reportGeneratorUtil.EndOfCoverageRunAsync();

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
                        await reportGeneratorUtil.LogCoverageProcessAsync("Initializing");
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

                await reportGeneratorUtil.LogCoverageProcessAsync("Starting coverage - full details in FCC Output Pane");
                LogReloadCoverageStatus(ReloadCoverageStatus.Start);

                var coverageProjects = await coverageRequestCallback();

                coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
                var reportOutputFolder = coverageOutputManager.GetReportOutputFolder();

                var coverOutputFiles = await RunCoverageAsync(coverageProjects, cancellationToken);

                if (coverOutputFiles.Any())
                {
                    var (lines, report) = await RunAndProcessReportAsync(coverOutputFiles, reportOutputFolder, cancellationToken);
                    coverageLines = lines;
                    reportHtml = report;
                }

                return (coverageLines, reportHtml);

            }, cancellationToken)
            .ContinueWith(DisplayCoverageResultAsync, System.Threading.Tasks.TaskScheduler.Default); 

        }
    }

}