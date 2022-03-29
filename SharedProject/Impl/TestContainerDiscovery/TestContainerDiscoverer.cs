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
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System.Diagnostics;

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
        private readonly Dictionary<TestOperationStates, Func<IOperation, Task>> testOperationStateChangeHandlers;
        private bool cancelling;
        private MsCodeCoverageCollectionStatus msCodeCoverageCollectionStatus;
        private bool runningInParallel;
        private IAppOptions settings;
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
            testOperationStateChangeHandlers = new Dictionary<TestOperationStates, Func<IOperation, Task>>
            {
                { TestOperationStates.TestExecutionCanceling, TestExecutionCancellingAsync},
                { TestOperationStates.TestExecutionStarting, TestExecutionStartingAsync},
                { TestOperationStates.TestExecutionFinished, TestExecutionFinishedAsync},
                { TestOperationStates.TestExecutionCancelAndFinished, TestExecutionCancelAndFinishedAsync},
            };

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
            ThreadHelper.JoinableTaskFactory.Run(taskProvider);
        };

        private async Task TestExecutionStartingAsync(IOperation operation)
        {
            cancelling = false;
            runningInParallel = false;
            StopCoverage();

            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
                CombinedLog("Coverage not collected as FCC disabled.");
                reportGeneratorUtil.EndOfCoverageRun();
                return;
            }

            msCodeCoverageCollectionStatus = await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperationFactory.Create(operation));
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

        private async Task TestExecutionFinishedAsync(IOperation operation)
        {
            var (should, testOperation) = ShouldConditionallyCollectWhenTestExecutionFinished(operation);
            if (should) {
                await TestExecutionFinishedCollectionAsync(operation, testOperation);
            }
            
        }

        private (bool should, ITestOperation testOperation) ShouldConditionallyCollectWhenTestExecutionFinished(IOperation operation)
        {
            if (ShouldNotCollectWhenTestExecutionFinished())
            {
                return (false, null);
            }
            
            var testOperation = testOperationFactory.Create(operation);
            
            var shouldCollect = CoverageConditionsMet(testOperation);
            return (shouldCollect, testOperation);
        }

        private bool ShouldNotCollectWhenTestExecutionFinished()
        {
            settings = appOptionsProvider.Get();
            return !settings.Enabled || runningInParallel || MsCodeCoverageErrored;
            
        }

        private async Task TestExecutionFinishedCollectionAsync(IOperation operation, ITestOperation testOperation)
        {
            if (msCodeCoverageCollectionStatus == MsCodeCoverageCollectionStatus.Collecting)
            {
                await msCodeCoverageRunSettingsService.CollectAsync(operation, testOperation);
            }
            else
            {
                fccEngine.ReloadCoverage(testOperation.GetCoverageProjectsAsync);
            }
        }

        private bool CoverageConditionsMet(ITestOperation testOperation)
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

        private Task CoverageCancelledAsync(string logMessage, IOperation operation)
        {
            CombinedLog(logMessage);
            reportGeneratorUtil.EndOfCoverageRun(); // not necessarily true but get desired result
            fccEngine.StopCoverage();
            return NotifyMsCodeCoverageTestExecutionNotFinishedIfCollectingAsync(operation);
        }

        private async Task NotifyMsCodeCoverageTestExecutionNotFinishedIfCollectingAsync(IOperation operation)
        {
            if (msCodeCoverageCollectionStatus == MsCodeCoverageCollectionStatus.Collecting)
            {
                var testOperation = testOperationFactory.Create(operation);
                await msCodeCoverageRunSettingsService.TestExecutionNotFinishedAsync(testOperation);
            }
        }

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            RunAsync(async () =>
            {
                await TryAndLogExceptionAsync(() => OperationState_StateChangedAsync(e));
            });
        }

        private async Task TestExecutionCancellingAsync(IOperation operation)
        {
            cancelling = true;
            await CoverageCancelledAsync("Test execution cancelling - running coverage will be cancelled.", operation);
        }

        private async Task TestExecutionCancelAndFinishedAsync(IOperation operation)
        {
            if (!cancelling)
            {
                await CoverageCancelledAsync("There has been an issue running tests. See the Tests output window pane.", operation);
            }
        }

        private async Task OperationState_StateChangedAsync(OperationStateChangedEventArgs e)
        {
            if (testOperationStateChangeHandlers.TryGetValue(e.State, out var handler)) {
                await handler(e.Operation);
            }
        }

        private async Task TryAndLogExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
                    
            }
            catch (Exception exception)
            {
                logger.Log("Error processing unit test events", exception);
            }
        }
    }
}
