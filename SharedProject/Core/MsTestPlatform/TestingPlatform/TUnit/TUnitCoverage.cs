using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Impl.TestContainerDiscovery;
using FineCodeCoverage.Output;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitCoverage))]
    [Export(typeof(ICoverageCollectableFromTestExplorer))]
    internal class TUnitCoverage : ITUnitCoverage, ICoverageCollectableFromTestExplorer
    {
        private readonly ITUnitProjectsProvider tUnitProjectsProvider;
        private readonly IBuildHelper buildHelper;
        private readonly ITUnitCoverageProjectFactory tUnitCoverageProjectFactory;
        private readonly ITUnitCoverageRunner tUnitCoverageRunner;
        private readonly ICoverageToolOutputManager coverageToolOutputManager;
        private readonly IFCCEngine fccEngine;
        private readonly ITUnitSettingsProvider tUnitSettingsProvider;
        private readonly IDisposeAwareTaskRunner disposeAwareTaskRunner;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger logger;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private int coverageRunNumber = 1;
        private ICancellationTokenSource cancellationTokenSource;
        private bool runnerReady;
        private bool projectsProviderReady;
        private bool externalBuildInProgress;

        public event EventHandler ReadyEvent;
        public event EventHandler<bool> CollectingChangedEvent;

        [ImportingConstructor]
        public TUnitCoverage(
            ITUnitProjectsProvider tUnitProjectsProvider,
            IBuildHelper buildHelper,
            ITUnitCoverageProjectFactory tUnitCoverageProjectFactory,
            ITUnitCoverageRunner tUnitCoverageRunner,
            ICoverageToolOutputManager coverageToolOutputManager,
            IFCCEngine fccEngine,
            ITUnitSettingsProvider tUnitSettingsProvider,
            IDisposeAwareTaskRunner disposeAwareTaskRunner,
            IEventAggregator eventAggregator,
            ILogger logger,
            IReportGeneratorUtil reportGeneratorUtil
        )
        {
            buildHelper.ExternalBuildEvent += BuildHelper_ExternalBuildEvent;
            tUnitProjectsProvider.ReadyEvent += TUnitProjectsProvider_ReadyEvent;
            tUnitCoverageRunner.ReadyEvent += TUnitRunner_ReadyEvent;
            projectsProviderReady = tUnitProjectsProvider.Ready;
            this.tUnitProjectsProvider = tUnitProjectsProvider;
            this.buildHelper = buildHelper;
            this.tUnitCoverageProjectFactory = tUnitCoverageProjectFactory;
            this.tUnitCoverageRunner = tUnitCoverageRunner;
            this.coverageToolOutputManager = coverageToolOutputManager;
            this.fccEngine = fccEngine;
            this.tUnitSettingsProvider = tUnitSettingsProvider;
            this.disposeAwareTaskRunner = disposeAwareTaskRunner;
            this.eventAggregator = eventAggregator;
            this.logger = logger;
            this.reportGeneratorUtil = reportGeneratorUtil;
        }

        private void BuildHelper_ExternalBuildEvent(object sender, BuildStartEndArgs e)
        {
            externalBuildInProgress = e.IsStart;
            OnReady();
        }

        private void TUnitRunner_ReadyEvent(object sender, EventArgs e)
        {
            runnerReady = true;
            OnReady();
        }

        private void TUnitProjectsProvider_ReadyEvent(object sender, EventArgs e)
        {
            projectsProviderReady = tUnitProjectsProvider.Ready;
            OnReady();
        }

        public bool Ready => runnerReady && projectsProviderReady && !externalBuildInProgress;
        private void OnReady()
        {
            ReadyEvent?.Invoke(this, EventArgs.Empty);
        }

        protected void OnCollectingChanged(bool collecting)
        {
            CollectingChangedEvent?.Invoke(this, collecting);
        }

        public void Cancel()
        {
            try
            {
                if(cancellationTokenSource?.IsCancellationRequested == false)
                {
                    cancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException) { }
        }

        public void CollectCoverage()
        {
            Cancel();
            cancellationTokenSource = disposeAwareTaskRunner.CreateLinkedTokenSource();
            disposeAwareTaskRunner.RunAsyncFunc(CollectCoverageAsync);
        }

        private void LogCoverageStarting()
        {
            reportGeneratorUtil.LogCoverageProcess($"Coverage Starting - {coverageRunNumber++}");
            logger.Log(StatusMarkerProvider.Get($"Coverage Starting - {coverageRunNumber++}"));
        }

        private async Task<List<ITUnitCoverageProject>> GetEnabledTUnitProjectsAsync(CancellationToken cancellationToken)
        {
            var tUnitProjects = await tUnitProjectsProvider.GetTUnitProjectsAsync(cancellationToken);
            var tUnitCoverageProjects = await Task.WhenAll(tUnitProjects.Select(tUnitProject => tUnitCoverageProjectFactory.CreateTUnitCoverageProjectAsync(tUnitProject, cancellationToken)));
            return tUnitCoverageProjects.Where(tp => tp.CoverageProject.Settings.Enabled).ToList();
        }

        private async Task CollectCoverageAsync()
        {
            var cancellationToken = cancellationTokenSource.Token;
            LogCoverageStarting();
            this.eventAggregator.SendMessage(new TestExecutionStartingMessage());

            OnCollectingChanged(true);//order important

            var raiseCoverageEndedMessage = true;
            try
            {
                var tUnitCoverageProjects = await GetEnabledTUnitProjectsAsync(cancellationToken);
                if (tUnitCoverageProjects.Any())
                {
                    var success = await BuildAndCollectAsync(tUnitCoverageProjects, cancellationToken);
                    raiseCoverageEndedMessage = !success;
                }
                else
                {
                    logger.Log("No enabled Tunit test projects.");
                }
            }
            catch (OperationCanceledException)
            {
                logger.Log("Coverage collection cancelled");
            }
            catch(Exception exc)
            {
                logger.Log(exc.ToString());
            }
            if (raiseCoverageEndedMessage)
            {
                reportGeneratorUtil.EndOfCoverageRun();
            }
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            OnCollectingChanged(false);
        }

        private async Task<bool> BuildAndCollectAsync(List<ITUnitCoverageProject> tUnitCoverageProjects, CancellationToken cancellationToken)
        {
            var success = false;
            logger.Log("Starting build");
            var buildSuccess = await buildHelper.BuildAsync(tUnitCoverageProjects.ConvertAll(tp => tp.VsHierarchy), cancellationToken);
            if (buildSuccess)
            {
                success = await CollectCoverageAsync(tUnitCoverageProjects, cancellationToken);
            }
            else
            {
                logger.Log("Unsuccessful build.  Not collecting coverage");
            }
            return success;
        }

        private async Task<bool> CollectCoverageAsync(List<ITUnitCoverageProject> tUnitCoverageProjects, CancellationToken cancellationToken)
        {
            logger.Log($"Collecting coverage for {tUnitCoverageProjects.Count} enabled TUnit test projects with coverage extension");

            var coverageProjects = tUnitCoverageProjects.ConvertAll(tUnitCoverageProject => tUnitCoverageProject.CoverageProject);
            cancellationToken.ThrowIfCancellationRequested();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);

            var runAllProjects = true;
            List<string> coberturaFiles = new List<string>();
            foreach (var tUnitCoverageProject in tUnitCoverageProjects)
            {
                var tUnitSettings = await tUnitSettingsProvider.ProvideAsync(tUnitCoverageProject, cancellationToken);
                var success = await tUnitCoverageRunner.RunAsync(tUnitSettings, tUnitCoverageProject.HasCoverageExtension, false, cancellationToken);
                if (success)
                {
                    coberturaFiles.Add(tUnitSettings.OutputPath);
                }
                else
                {
                    runAllProjects = false;
                    break;
                }
            }

            if (runAllProjects)
            {
                fccEngine.RunAndProcessReport(coberturaFiles.ToArray(), null);
            }
            else
            {
                logger.Log("Not collecting coverage due to unsuccessful test");
            }
            return runAllProjects;
        }

        async Task<bool> ICoverageCollectableFromTestExplorer.IsCollectableAsync()
        {
            var tunitProjects =  await tUnitProjectsProvider.GetTUnitProjectsAsync(CancellationToken.None);
            return !tunitProjects.Any();
        }
    }
}
