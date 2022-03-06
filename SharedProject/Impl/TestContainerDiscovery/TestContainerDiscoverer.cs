using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Task = System.Threading.Tasks.Task;
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
        private bool cancelling;
        internal Task initializeTask;

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
            IReportGeneratorUtil reportGeneratorUtil,
            IDisposeAwareTaskRunner disposeAwareTaskRunner 

        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.reportGeneratorUtil = reportGeneratorUtil;
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
            fccEngine.StopCoverage();

            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
                CombinedLog("Coverage not collected as FCC disabled.");
                reportGeneratorUtil.EndOfCoverageRun();
                return;
            }            
            if (settings.MsCodeCoverage)
            {
                var testOperation = testOperationFactory.Create(operation);
                fccEngine.PrepareTestRun(testOperation);
            }
            else if (settings.RunInParallel)
            {
                fccEngine.ReloadCoverage(() =>
                {
                    var testOperation = testOperationFactory.Create(operation);
                    return testOperation.GetCoverageProjectsAsync();

                });
            }
            else
            {
                CombinedLog("Coverage collected when tests finish. RunInParallel option true for immediate");
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
            if (!settings.Enabled || settings.RunInParallel)
            {
                return;
            }
            var testOperation = testOperationFactory.Create(operation);
            if (!settings.RunWhenTestsFail && testOperation.FailedTests > 0)
            {
                CombinedLog($"Skipping coverage due to failed tests.  Option {nameof(AppOptions.RunWhenTestsFail)} is false");
                reportGeneratorUtil.EndOfCoverageRun();
                return;
            }

            var totalTests = testOperation.TotalTests;
            var runWhenTestsExceed = settings.RunWhenTestsExceed;
            if (totalTests > 0) // in case this changes to not reporting total tests
            {
                if (totalTests <= runWhenTestsExceed)
                {
                    CombinedLog($"Skipping coverage as total tests ({totalTests}) <= {nameof(AppOptions.RunWhenTestsExceed)} ({runWhenTestsExceed})");
                    reportGeneratorUtil.EndOfCoverageRun();
                    return;
                }
            }
            fccEngine.ReloadCoverage(testOperation.GetCoverageProjectsAsync);
        }
        
        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            try
            {
                if (e.State == TestOperationStates.TestExecutionCanceling)
                {
                    cancelling = true;
                    CombinedLog("Test execution cancelling - running coverage will be cancelled.");
                    reportGeneratorUtil.EndOfCoverageRun(); // not necessarily true but get desired result
                    fccEngine.StopCoverage();
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
                    CombinedLog("There has been an issue running tests. See the Tests output window pane.");
                    reportGeneratorUtil.EndOfCoverageRun(); // not necessarily true but get desired result
                    fccEngine.StopCoverage();
                }
                    
            }
            catch (Exception exception)
            {
                logger.Log("Error processing unit test events", exception);
            }
            
        }
    }
}
