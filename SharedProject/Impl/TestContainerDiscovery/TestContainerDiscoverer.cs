using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Utilities;
using FineCodeCoverage.Engine.MsTestPlatform;

namespace FineCodeCoverage.Impl
{
    [Name(Vsix.TestContainerDiscovererName)]
    // Both exports necessary !
    [Export(typeof(TestContainerDiscoverer))]
    [Export(typeof(ITestContainerDiscoverer))]
    internal class TestContainerDiscoverer : ITestContainerDiscoverer
    {
#pragma warning disable 67
        public event EventHandler TestContainersUpdated;
#pragma warning restore 67
        private readonly IFCCEngine fccEngine;
        private readonly ITestOperationFactory testOperationFactory;
        private readonly ILogger logger;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private bool cancelling;
        private MsCodeCoverageCollectionStatus msCodeCoverageCollectionStatus;
        private bool runningInParallel;
        internal Task initializeTask;

        [ExcludeFromCodeCoverage]
        public Uri ExecutorUri => new Uri($"executor://{Vsix.Code}.Executor/v1");
        [ExcludeFromCodeCoverage]
        public IEnumerable<ITestContainer> TestContainers => Enumerable.Empty<ITestContainer>();
        public bool MsCodeCoverageErrored => msCodeCoverageCollectionStatus == MsCodeCoverageCollectionStatus.Error;

        [ImportingConstructor]
        public TestContainerDiscoverer
        (
            [Import(typeof(IOperationState))]
            IOperationState operationState,

            IFCCEngine fccEngine,
            IInitializer initializer,
            ITestOperationFactory testOperationFactory,
            ILogger logger,
            IAppOptionsProvider appOptionsProvider,
            IReportGeneratorUtil reportGeneratorUtil,
            IDisposeAwareTaskRunner disposeAwareTaskRunner,
            IMsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService

        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.msCodeCoverageRunSettingsService = msCodeCoverageRunSettingsService;
            this.fccEngine = fccEngine;
            this.testOperationFactory = testOperationFactory;
            this.logger = logger;

            disposeAwareTaskRunner.RunAsync(() =>
            {
                initializeTask = Task.Run(async () =>
                {
                    operationState.StateChanged += OperationState_StateChanged;
                    await initializer.InitializeAsync(disposeAwareTaskRunner.DisposalToken);
                });
                return initializeTask;
            });
        }

        internal Action<Func<System.Threading.Tasks.Task>> RunAsync = (taskProvider) =>
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(taskProvider);
        };

        private void TestExecutionStarting(IOperation operation)
        {
            runningInParallel = false;
            StopCoverage();

            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
                CombinedLog("Coverage not collected as FCC disabled.");
                reportGeneratorUtil.EndOfCoverageRun();
                return;
            }

            msCodeCoverageCollectionStatus = msCodeCoverageRunSettingsService.IsCollecting(testOperationFactory.Create(operation));
            if (msCodeCoverageCollectionStatus == MsCodeCoverageCollectionStatus.NotCollecting)
            {
                if (settings.RunInParallel)
                {
                    runningInParallel = true;
                    fccEngine.ReloadCoverage(() =>
                    {
                        return testOperationFactory.Create(operation).GetCoverageProjectsAsync();

                    });
                }
                else
                {
                    CombinedLog("Coverage collected when tests finish. RunInParallel option true for immediate");
                }
            }
        }

        private void CombinedLog(string message)
        {
            reportGeneratorUtil.LogCoverageProcess(message);
            logger.Log(message);
        }

        private void TestExecutionFinished(IOperation operation)
        {
            var settings = appOptionsProvider.Get();
            if (!settings.Enabled || runningInParallel || MsCodeCoverageErrored)
            {
                return;
            }

            var testOperation = testOperationFactory.Create(operation);

            if(!CoverageConditionsMet(testOperation, settings))
            {
                return;
            }
            
            if (msCodeCoverageCollectionStatus == MsCodeCoverageCollectionStatus.Collecting)
            {
                msCodeCoverageRunSettingsService.Collect(operation, testOperation);
            }
            else
            {
                fccEngine.ReloadCoverage(testOperation.GetCoverageProjectsAsync);
            }
        }

        private bool CoverageConditionsMet(ITestOperation testOperation, IAppOptions settings)
        {
            if (!settings.RunWhenTestsFail && testOperation.FailedTests > 0)
            {
                CombinedLog($"Skipping coverage due to failed tests.  Option {nameof(AppOptions.RunWhenTestsFail)} is false");
                reportGeneratorUtil.EndOfCoverageRun();
                return false;
            }

            var totalTests = testOperation.TotalTests;
            var runWhenTestsExceed = settings.RunWhenTestsExceed;
            if (totalTests > 0) // in case this changes to not reporting total tests
            {
                if (totalTests <= runWhenTestsExceed)
                {
                    CombinedLog($"Skipping coverage as total tests ({totalTests}) <= {nameof(AppOptions.RunWhenTestsExceed)} ({runWhenTestsExceed})");
                    reportGeneratorUtil.EndOfCoverageRun();
                    return false;
                }
            }
            return true;
        }
        
        private void StopCoverage()
        {
            switch (msCodeCoverageCollectionStatus)
            {
                case MsCodeCoverageCollectionStatus.Collecting:
                    msCodeCoverageRunSettingsService.StopCoverage();
                    break;
                case MsCodeCoverageCollectionStatus.NotCollecting:
                    fccEngine.StopCoverage();
                    break;
            }
        }

        private void CoverageCancelled(string logMessage)
        {
            CombinedLog(logMessage);
            reportGeneratorUtil.EndOfCoverageRun(); // not necessarily true but get desired result
            fccEngine.StopCoverage();
            // not necessary for ms code coverage as only runs when TestExecutionFinished
        }

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            try
            {
                if (e.State == TestOperationStates.TestExecutionCanceling)
                {
                    cancelling = true;
                    CoverageCancelled("Test execution cancelling - running coverage will be cancelled.");
                }


                if (e.State == TestOperationStates.TestExecutionStarting)
                {                    
                    TestExecutionStarting(e.Operation);
                    cancelling = false;
                }

                if (e.State == TestOperationStates.TestExecutionFinished)
                {                    
                    TestExecutionFinished(e.Operation);                    
                }

                if (e.State == TestOperationStates.TestExecutionCancelAndFinished && !cancelling)
                {
                    CoverageCancelled("There has been an issue running tests. See the Tests output window pane.");
                }
                    
            }
            catch (Exception exception)
            {
                logger.Log("Error processing unit test events", exception);
            }
            
        }
    }
}
