using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.Utilities;

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
        internal System.Threading.Thread initializeThread;

        [ExcludeFromCodeCoverage]
        public Uri ExecutorUri => new Uri($"executor://{Vsix.Code}.Executor/v1");
        [ExcludeFromCodeCoverage]
        public IEnumerable<ITestContainer> TestContainers => Enumerable.Empty<ITestContainer>();


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
            IReportGeneratorUtil reportGeneratorUtil

        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.fccEngine = fccEngine;
            this.testOperationFactory = testOperationFactory;
            this.logger = logger;
            
            initializeThread = new Thread(() =>
            {
                operationState.StateChanged += OperationState_StateChanged;
                _ = initializer.InitializeAsync();
            });
            initializeThread.Start();
            
        }

        private async System.Threading.Tasks.Task TestExecutionStartingAsync(IOperation operation)
        {
            fccEngine.StopCoverage();

            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
                await CombinedLogAsync("Coverage not collected as FCC disabled.");
                await reportGeneratorUtil.EndOfCoverageRunAsync();
                return;
            }
            if (settings.RunInParallel)
            {
                fccEngine.ReloadCoverage(() =>
                {
                    var testOperation = testOperationFactory.Create(operation);
                    return testOperation.GetCoverageProjectsAsync();

                });
            }
            else
            {
                await CombinedLogAsync("Coverage collected when tests finish. RunInParallel option true for immediate");
            }
        }

        private async System.Threading.Tasks.Task CombinedLogAsync(string message)
        {
            await reportGeneratorUtil.LogCoverageProcessAsync(message);
            logger.Log(message);
        }

        private async System.Threading.Tasks.Task TestExecutionFinishedAsync(IOperation operation)
        {
            var settings = appOptionsProvider.Get();
            if (!settings.Enabled || settings.RunInParallel)
            {
                return;
            }
            var testOperation = testOperationFactory.Create(operation);
            if (!settings.RunWhenTestsFail && testOperation.FailedTests > 0)
            {
                await CombinedLogAsync($"Skipping coverage due to failed tests.  Option {nameof(AppOptions.RunWhenTestsFail)} is false");
                await reportGeneratorUtil.EndOfCoverageRunAsync();
                return;
            }

            var totalTests = testOperation.TotalTests;
            var runWhenTestsExceed = settings.RunWhenTestsExceed;
            if (totalTests > 0) // in case this changes to not reporting total tests
            {
                if (totalTests <= runWhenTestsExceed)
                {
                    await CombinedLogAsync($"Skipping coverage as total tests ({totalTests}) <= {nameof(AppOptions.RunWhenTestsExceed)} ({runWhenTestsExceed})");
                    await reportGeneratorUtil.EndOfCoverageRunAsync();
                    return;
                }
            }
            fccEngine.ReloadCoverage(testOperation.GetCoverageProjectsAsync);
        }

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    if (e.State == TestOperationStates.TestExecutionCanceling)
                    {
                        await CombinedLogAsync("Test execution cancelling - running coverage will be cancelled.");
                        await reportGeneratorUtil.EndOfCoverageRunAsync(); // not necessarily true but get desired result
                        fccEngine.StopCoverage();
                    }


                    if (e.State == TestOperationStates.TestExecutionStarting)
                    {
                        await TestExecutionStartingAsync(e.Operation);
                    }

                    if (e.State == TestOperationStates.TestExecutionFinished)
                    {
                        await TestExecutionFinishedAsync(e.Operation);
                    }

                    if (e.State == TestOperationStates.TestExecutionCancelAndFinished)
                    {
                        await CombinedLogAsync("There has been an issue running tests. See the Tests output window pane.");
                        await reportGeneratorUtil.EndOfCoverageRunAsync(); // not necessarily true but get desired result
                    fccEngine.StopCoverage();
                    }
                }
                catch (Exception exception)
                {
                    logger.Log("Error processing unit test events", exception);
                }
            });
            
        }
    }
}
