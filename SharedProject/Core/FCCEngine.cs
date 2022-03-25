using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Engine
{
    internal enum ReloadCoverageStatus { Start, Done, Cancelled, Error, Initializing };

    internal class DisplayCoverageResultState
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public Action CleanUp { get; set; }
    }

    [Export(typeof(IFCCEngine))]
    internal class FCCEngine : IFCCEngine,IDisposable
    {
        internal int InitializeWait { get; set; } = 5000;
        internal const string initializationFailedMessagePrefix = "Initialization failed.  Please check the following error which may be resolved by reopening visual studio which will start the initialization process again.";
        private CancellationTokenSource cancellationTokenSource;

        public event UpdateMarginTagsDelegate UpdateMarginTags;
        
        public string AppDataFolderPath { get; private set; }
        public List<CoverageLine> CoverageLines { get; internal set; }
        private bool IsVsShutdown => disposeAwareTaskRunner.DisposalToken.IsCancellationRequested;

        private readonly ICoverageUtilManager coverageUtilManager;
        private readonly ICoberturaUtil coberturaUtil;        
        private readonly IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;

        private IInitializeStatusProvider initializeStatusProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        internal System.Threading.Tasks.Task reloadCoverageTask;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ISolutionEvents solutionEvents; // keep alive
#pragma warning restore IDE0052 // Remove unread private members
        private readonly IEventAggregator eventAggregator;
        private readonly IDisposeAwareTaskRunner disposeAwareTaskRunner;
        private bool disposed = false;

        [ImportingConstructor]
        public FCCEngine(
            ICoverageUtilManager coverageUtilManager,
            ICoberturaUtil coberturaUtil,
            IMsTestPlatformUtil msTestPlatformUtil,            
            IReportGeneratorUtil reportGeneratorUtil,
            ILogger logger,
            IAppDataFolder appDataFolder,
            ICoverageToolOutputManager coverageOutputManager,
            IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService,
            ISolutionEvents solutionEvents,
            IAppOptionsProvider appOptionsProvider,
            IEventAggregator eventAggregator,
            IDisposeAwareTaskRunner disposeAwareTaskRunner
            )
        {
            this.solutionEvents = solutionEvents;
            this.eventAggregator = eventAggregator;
            this.disposeAwareTaskRunner = disposeAwareTaskRunner;
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
            this.logger = logger;
            this.appDataFolder = appDataFolder;
            this.msCodeCoverageRunSettingsService = msCodeCoverageRunSettingsService;
        }

        internal string GetLogReloadCoverageStatusMessage(ReloadCoverageStatus reloadCoverageStatus)
        {
            return $"================================== {reloadCoverageStatus.ToString().ToUpper()} ==================================";
        }
        private void LogReloadCoverageStatus(ReloadCoverageStatus reloadCoverageStatus)
        {
            logger.Log(GetLogReloadCoverageStatusMessage(reloadCoverageStatus));
        }

        public void Initialize(IInitializeStatusProvider initializeStatusProvider, CancellationToken cancellationToken)
        {
            this.initializeStatusProvider = initializeStatusProvider;

            appDataFolder.Initialize(cancellationToken);
            AppDataFolderPath = appDataFolder.DirectoryPath;

            reportGeneratorUtil.Initialize(AppDataFolderPath, cancellationToken);
            msTestPlatformUtil.Initialize(AppDataFolderPath, cancellationToken);
            coverageUtilManager.Initialize(AppDataFolderPath, cancellationToken);
            msCodeCoverageRunSettingsService.Initialize(AppDataFolderPath, this,cancellationToken);
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
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException) { }
            }
        }
        
        private CancellationTokenSource Reset()
        {
            CoverageLines = null;
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs());

            StopCoverage();

            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposeAwareTaskRunner.DisposalToken);

            return cancellationTokenSource;
        }

        private async System.Threading.Tasks.Task<string[]> RunCoverageAsync(List<ICoverageProject> coverageProjects,CancellationToken vsShutdownLinkedCancellationToken)
        {
            // process pipeline

            await PrepareCoverageProjectsAsync(coverageProjects, vsShutdownLinkedCancellationToken);

            foreach (var coverageProject in coverageProjects)
            {
                await coverageProject.StepAsync("Run Coverage Tool", async (project) =>
                {
                    var start = DateTime.Now;
                    
                    var coverageTool = coverageUtilManager.CoverageToolName(project);
                    var runCoverToolMessage = $"Run {coverageTool} ({project.ProjectName})";
                    logger.Log(runCoverToolMessage);
                    reportGeneratorUtil.LogCoverageProcess(runCoverToolMessage);
                    await coverageUtilManager.RunCoverageAsync(project, vsShutdownLinkedCancellationToken);
                    
                    var duration = DateTime.Now - start;
                    var durationMessage = $"Completed coverage for ({coverageProject.ProjectName}) : {duration}";
                    logger.Log(durationMessage);
                    reportGeneratorUtil.LogCoverageProcess(durationMessage);
                    
                });

                if (coverageProject.HasFailed)
                {
                    var coverageStagePrefix = String.IsNullOrEmpty(coverageProject.FailureStage) ? "" : $"{coverageProject.FailureStage} ";
                    var failureMessage = $"{coverageProject.FailureStage}({coverageProject.ProjectName}) Failed.";
                    logger.Log(failureMessage, coverageProject.FailureDescription);
                    reportGeneratorUtil.LogCoverageProcess(failureMessage + "  See the FCC Output Pane");
                }

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

        private async System.Threading.Tasks.Task<(List<CoverageLine> coverageLines,string reportFilePath)> RunAndProcessReportAsync(string[] coverOutputFiles, CancellationToken vsShutdownLinkedCancellationToken)
        {
            var reportOutputFolder = coverageOutputManager.GetReportOutputFolder();
            vsShutdownLinkedCancellationToken.ThrowIfCancellationRequested();
            var result = await reportGeneratorUtil.GenerateAsync(coverOutputFiles,reportOutputFolder,vsShutdownLinkedCancellationToken);

            vsShutdownLinkedCancellationToken.ThrowIfCancellationRequested();
            logger.Log("Processing cobertura");
            var coverageLines = coberturaUtil.ProcessCoberturaXml(result.UnifiedXmlFile);

            vsShutdownLinkedCancellationToken.ThrowIfCancellationRequested();
            logger.Log("Processing report");
            string processedReport = reportGeneratorUtil.ProcessUnifiedHtml(result.UnifiedHtml, reportOutputFolder);
            return (coverageLines, processedReport);
        }

        private async System.Threading.Tasks.Task PrepareCoverageProjectsAsync(List<ICoverageProject> coverageProjects, CancellationToken cancellationToken)
        {
            foreach (var project in coverageProjects)
            {
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

                var fileSynchronizationDetails = await project.PrepareForCoverageAsync(cancellationToken);
                var logs = fileSynchronizationDetails.Logs;
                if (logs.Any())
                {
                    logs.Add($"File synchronization duration : {fileSynchronizationDetails.Duration}");
                    logger.Log(logs);
                    
                    var itemOrItems = logs.Count == 1 ? "item" : "items";
                    reportGeneratorUtil.LogCoverageProcess($"File synchronization {logs.Count} {itemOrItems}, duration : {fileSynchronizationDetails.Duration}");
                }
            }
        }

        private void DisplayCoverageResult(System.Threading.Tasks.Task<(List<CoverageLine> coverageLines, string reportHtml)> t, object state)
        {
            var displayCoverageResultState = (DisplayCoverageResultState)state;
            if (!IsVsShutdown)
            {
                switch (t.Status)
                {
                    case System.Threading.Tasks.TaskStatus.Canceled:
                        LogReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
                        reportGeneratorUtil.LogCoverageProcess("Coverage cancelled");
                        break;
                    case System.Threading.Tasks.TaskStatus.Faulted:
                        var innerException = t.Exception.InnerExceptions[0];
                        logger.Log(
                            GetLogReloadCoverageStatusMessage(ReloadCoverageStatus.Error),
                            innerException
                        );
                        reportGeneratorUtil.LogCoverageProcess("An exception occurred. See the FCC Output Pane");
                        break;
                    case System.Threading.Tasks.TaskStatus.RanToCompletion:
                        LogReloadCoverageStatus(ReloadCoverageStatus.Done);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                        UpdateUI(t.Result.coverageLines, t.Result.reportHtml);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        break;
                }

                reportGeneratorUtil.EndOfCoverageRun();
            }
            displayCoverageResultState.CleanUp?.Invoke();
            displayCoverageResultState.CancellationTokenSource.Dispose();
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
                        reportGeneratorUtil.LogCoverageProcess("Initializing");
                        LogReloadCoverageStatus(ReloadCoverageStatus.Initializing);
                        await System.Threading.Tasks.Task.Delay(InitializeWait);
                        break;
                    case InitializeStatus.Error:
                        throw new Exception(initializationFailedMessagePrefix + Environment.NewLine + initializeStatusProvider.InitializeExceptionMessage);
                }
            }
        }

        public void RunAndProcessReport(string[] coberturaFiles, Action cleanUp = null)
        {
            RunCancellableCoverageTask(async (vsShutdownLinkedCancellationToken) =>
            {
                List<CoverageLine> coverageLines = null;
                string reportHtml = null;

                if (coberturaFiles.Any())
                {
                    (coverageLines, reportHtml) = await RunAndProcessReportAsync(coberturaFiles, vsShutdownLinkedCancellationToken);
                }
                return (coverageLines, reportHtml);
            }, cleanUp);
        }

        private void RunCancellableCoverageTask(
            Func<CancellationToken,System.Threading.Tasks.Task<(List<CoverageLine>, string)>> taskCreator, Action cleanUp)
        {
            var vsLinkedCancellationTokenSource = Reset();
            var vsShutdownLinkedCancellationToken = vsLinkedCancellationTokenSource.Token;
            disposeAwareTaskRunner.RunAsync(() =>
            {
                reloadCoverageTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    await PollInitializedStatusAsync(vsShutdownLinkedCancellationToken);
                    var result = await taskCreator(vsShutdownLinkedCancellationToken);
                    return result;

                }, vsShutdownLinkedCancellationToken)
                .ContinueWith(DisplayCoverageResult, new DisplayCoverageResultState { CancellationTokenSource = vsLinkedCancellationTokenSource, CleanUp = cleanUp}, System.Threading.Tasks.TaskScheduler.Default);
                return reloadCoverageTask;
            });
        }

        public void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback)
        {
            RunCancellableCoverageTask(async (vsShutdownLinkedCancellationToken) =>
            {
                List<CoverageLine> coverageLines = null;
                string reportHtml = null;

                await PollInitializedStatusAsync(vsShutdownLinkedCancellationToken);

                reportGeneratorUtil.LogCoverageProcess("Starting coverage - full details in FCC Output Pane");
                LogReloadCoverageStatus(ReloadCoverageStatus.Start);

                var coverageProjects = await coverageRequestCallback();
                vsShutdownLinkedCancellationToken.ThrowIfCancellationRequested();

                coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
                

                var coverOutputFiles = await RunCoverageAsync(coverageProjects, vsShutdownLinkedCancellationToken);
                if (coverOutputFiles.Any())
                {
                    (coverageLines, reportHtml) = await RunAndProcessReportAsync(coverOutputFiles, vsShutdownLinkedCancellationToken);
                }

                return (coverageLines, reportHtml);
            },null);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing && cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                }

                disposed = true;
            }
        }
    }

}