using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
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
        internal const string errorReadingReportGeneratorOutputMessage = "error reading report generator output";
        private readonly object colorThemeService;
        private string CurrentTheme => $"{((dynamic)colorThemeService)?.CurrentTheme?.Name}".Trim();

        private CancellationTokenSource cancellationTokenSource;
        
        public event UpdateMarginTagsDelegate UpdateMarginTags;
        public event UpdateOutputWindowDelegate UpdateOutputWindow;
        
        public string AppDataFolderPath { get; private set; }
        private readonly ICoverletUtil coverletUtil;
        private readonly IOpenCoverUtil openCoverUtil;
        private readonly ICoberturaUtil coberturaUtil;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly IProcessUtil processUtil;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;
        private readonly IServiceProvider serviceProvider;
        private IInitializeStatusProvider initializeStatusProvider;
        internal System.Threading.Tasks.Task reloadCoverageTask;

        [ImportingConstructor]
        public FCCEngine(
            ICoverletUtil coverletUtil,
            IOpenCoverUtil openCoverUtil,
            ICoberturaUtil coberturaUtil,
            IMsTestPlatformUtil msTestPlatformUtil,
            IReportGeneratorUtil reportGeneratorUtil,
            IProcessUtil processUtil,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger,
            IAppDataFolder appDataFolder,
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
            this.coverletUtil = coverletUtil;
            this.openCoverUtil = openCoverUtil;
            this.coberturaUtil = coberturaUtil;
            this.msTestPlatformUtil = msTestPlatformUtil;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.processUtil = processUtil;
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.appDataFolder = appDataFolder;
            this.serviceProvider = serviceProvider;
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
            openCoverUtil.Initialize(AppDataFolderPath);
            coverletUtil.Initialize(AppDataFolderPath);
        }

        public void ClearUI()
        {
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs { CoverageLines = new List<CoverageLine>()});

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
            StopCoverage();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            processUtil.CancellationToken = cancellationToken;

            return cancellationToken;
        }

        private System.Threading.Tasks.Task RunCoverToolAsync(ICoverageProject project)
        {
            if (project.IsDotNetSdkStyle())
            {
                return coverletUtil.RunCoverletAsync(project, true);
            }
            else
            {
                return openCoverUtil.RunOpenCoverAsync(project, true);
            }
        }

        private async System.Threading.Tasks.Task<string[]> RunCoverageAsync(List<ICoverageProject> coverageProjects,CancellationToken cancellationToken)
        {
            // process pipeline

            await PrepareCoverageProjectsAsync(coverageProjects, cancellationToken);

            foreach (var coverageProject in coverageProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await coverageProject.StepAsync("Run Coverage Tool", RunCoverToolAsync);
            }

            var passedProjects = coverageProjects.Where(p => !p.HasFailed);

            return passedProjects
                    .Select(x => x.CoverageOutputFile)
                    .ToArray();

        }

        private void RaiseUpdateOutputWindow(string reportFilePath)
        {
            UpdateOutputWindowEventArgs updateOutputWindowEventArgs = new UpdateOutputWindowEventArgs { };

            try
            {
                if (!string.IsNullOrEmpty(reportFilePath))
                {
                    var htmlContent = File.ReadAllText(reportFilePath);
                    updateOutputWindowEventArgs.HtmlContent = htmlContent;
                }
            }
            catch
            {
                logger.Log(errorReadingReportGeneratorOutputMessage);
            }
            finally
            {
                UpdateOutputWindow?.Invoke(updateOutputWindowEventArgs);
            }
        }
        private void UpdateUI(List<CoverageLine> coverageLines, string reportFilePath)
        {
            UpdateMarginTags?.Invoke(new UpdateMarginTagsEventArgs { CoverageLines = coverageLines});
            RaiseUpdateOutputWindow(reportFilePath);
        }

        private async System.Threading.Tasks.Task<(List<CoverageLine> coverageLines,string reportFilePath)> RunAndProcessReportAsync(string[] coverOutputFiles,CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<CoverageLine> coverageLines = null;
            string reportFilePath = null;
            
            var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

            var result = await reportGeneratorUtil.RunReportGeneratorAsync(coverOutputFiles, darkMode, true);

            if (result.Success)
            {
                coberturaUtil.ProcessCoberturaXmlFile(result.UnifiedXmlFile);
                coverageLines = coberturaUtil.CoverageLines;

                reportGeneratorUtil.ProcessUnifiedHtmlFile(result.UnifiedHtmlFile, darkMode, out var htmlFilePath);
                reportFilePath = htmlFilePath;
            }
            return (coverageLines, reportFilePath);
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

        private void ReloadCoverageTaskContinuation(System.Threading.Tasks.Task<(List<CoverageLine> coverageLines, string reportFilePath)> t)
        {
            switch (t.Status)
            {
                case System.Threading.Tasks.TaskStatus.Canceled:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
                    break;
                case System.Threading.Tasks.TaskStatus.Faulted:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Error);
                    logger.Log(t.Exception.InnerExceptions[0]);
                    UpdateUI(null, null);
                    break;
                case System.Threading.Tasks.TaskStatus.RanToCompletion:
                    LogReloadCoverageStatus(ReloadCoverageStatus.Done);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    UpdateUI(t.Result.coverageLines, t.Result.reportFilePath);
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
                string reportFilePath = null;

                await PollInitializedStatusAsync(cancellationToken);

                LogReloadCoverageStatus(ReloadCoverageStatus.Start);

                var coverageProjects = await coverageRequestCallback();

                var coverOutputFiles = await RunCoverageAsync(coverageProjects, cancellationToken);

                if (coverOutputFiles.Any())
                {
                    var (lines, rFilePath) = await RunAndProcessReportAsync(coverOutputFiles,cancellationToken);
                    coverageLines = lines;
                    reportFilePath = rFilePath;
                }

                return (coverageLines, reportFilePath);
                 
            }, cancellationToken)
            .ContinueWith(t =>
            {
                ReloadCoverageTaskContinuation(t);

              }, System.Threading.Tasks.TaskScheduler.Default);

        }

    }

}