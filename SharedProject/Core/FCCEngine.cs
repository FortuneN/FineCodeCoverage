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
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine
{
    internal enum ReloadCoverageStatus { Start, Done, Cancelled, Error, Initializing };

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
        public string SolutionPath { get; set; }


        private readonly ICoverageUtilManager coverageUtilManager;
        private readonly ICoberturaUtil coberturaUtil;        
        private readonly IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;
        private readonly IServiceProvider serviceProvider;

        private IInitializeStatusProvider initializeStatusProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        internal System.Threading.Tasks.Task reloadCoverageTask;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ISolutionEvents solutionEvents; // keep alive
#pragma warning restore IDE0052 // Remove unread private members
        private readonly IEventAggregator eventAggregator;
        private readonly IDisposeAwareTaskRunner disposeAwareTaskRunner;
        private readonly IAppOptionsProvider appOptionsProvider;
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
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
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
            this.appOptionsProvider = appOptionsProvider;
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
            this.serviceProvider = serviceProvider;            
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
            msCodeCoverageRunSettingsService.Initialize(AppDataFolderPath, cancellationToken);
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

        private async System.Threading.Tasks.Task<string[]> RunCoverageAsync(List<ICoverageProject> coverageProjects,CancellationToken cancellationToken)
        {
            // process pipeline

            await PrepareCoverageProjectsAsync(coverageProjects, cancellationToken);

            foreach (var coverageProject in coverageProjects)
            {
                await coverageProject.StepAsync("Run Coverage Tool", async (project) =>
                {
                    var start = DateTime.Now;
                    
                    var coverageTool = coverageUtilManager.CoverageToolName(project);
                    var runCoverToolMessage = $"Run {coverageTool} ({project.ProjectName})";
                    logger.Log(runCoverToolMessage);
                    reportGeneratorUtil.LogCoverageProcess(runCoverToolMessage);
                    await coverageUtilManager.RunCoverageAsync(project, cancellationToken);
                    
                    var duration = DateTime.Now - start;
                    var durationMessage = $"Completed coverage for ({coverageProject.ProjectName}) : {duration}";
                    logger.Log(durationMessage);
                    reportGeneratorUtil.LogCoverageProcess(durationMessage);
                    
                });

                if (coverageProject.HasFailed)
                {
                    var coverageStagePrefix = String.IsNullOrEmpty(coverageProject.FailureStage) ? "" : $"{coverageProject.FailureStage} ";
                    var failureMessage = $"{coverageProject.FailureStage}({coverageProject.ProjectName}) Failed";
                    logger.Log(failureMessage, coverageProject.FailureDescription);
                    reportGeneratorUtil.LogCoverageProcess(failureMessage + Environment.NewLine + coverageProject.FailureDescription);
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

        private async System.Threading.Tasks.Task<(List<CoverageLine> coverageLines,string reportFilePath)> RunAndProcessReportAsync(string[] coverOutputFiles, string reportOutputFolder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await reportGeneratorUtil.GenerateAsync(coverOutputFiles,reportOutputFolder,cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            logger.Log("Processing cobertura");
            var coverageLines = coberturaUtil.ProcessCoberturaXml(result.UnifiedXmlFile);

            cancellationToken.ThrowIfCancellationRequested();
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

        private void DisplayCoverageResult(System.Threading.Tasks.Task<(List<CoverageLine> coverageLines, string reportHtml)> t, object cts)
        {
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
                        reportGeneratorUtil.LogCoverageProcess(innerException.ToString());
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
            
            ((CancellationTokenSource)cts).Dispose();
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

        public void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback)
        {
            var cts = Reset();
            var cancellationToken = cts.Token;
            disposeAwareTaskRunner.RunAsync(() =>
              {
                  reloadCoverageTask = System.Threading.Tasks.Task.Run(async () =>
                  {
                      List<CoverageLine> coverageLines = null;
                      string reportHtml = null;

                      await PollInitializedStatusAsync(cancellationToken);

                      reportGeneratorUtil.LogCoverageProcess("Starting coverage - full details in FCC Output Pane");
                      LogReloadCoverageStatus(ReloadCoverageStatus.Start);

                      var coverageProjects = await coverageRequestCallback();
                      cancellationToken.ThrowIfCancellationRequested();
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
                    .ContinueWith(DisplayCoverageResult,cts, System.Threading.Tasks.TaskScheduler.Default);
                  return reloadCoverageTask;

              });
            

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
                if (disposing)
                {
                    cancellationTokenSource.Dispose();
                }

                disposed = true;
            }
        }

        public void PrepareTestRun(ITestOperation testOperation)
        {
            msCodeCoverageRunSettingsService.PrepareRunSettings(SolutionPath, testOperation);
        }

    }

}