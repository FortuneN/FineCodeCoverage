using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Impl
{
    [Name(Vsix.TestContainerDiscovererName)]
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
            IAppOptionsProvider appOptionsProvider

        )
        {
            appOptionsProvider.OptionsChanged += AppOptionsEvents_OptionsChanged;
            this.appOptionsProvider = appOptionsProvider;
            this.fccEngine = fccEngine;
            this.testOperationFactory = testOperationFactory;
            this.logger = logger;
            
            initializeThread = new Thread(() =>
            {
                operationState.StateChanged += OperationState_StateChanged;
                initializer.Initialize();
            });
            initializeThread.Start();
            
        }

        private void AppOptionsEvents_OptionsChanged(IAppOptions appOptions)
        {
            if (!appOptions.Enabled)
            {
                fccEngine.ClearUI();
            }
        }
        private void TestExecutionStarting(IOperation operation)
        {
            fccEngine.StopCoverage();

            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
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
        }

        private void TestExecutionFinished(IOperation operation)
        {
            var settings = appOptionsProvider.Get();
            if (!settings.Enabled)
            {
                return;
            }
            if (settings.RunInParallel)
            {
                return;
            }
            var testOperation = testOperationFactory.Create(operation);
            if (!settings.RunWhenTestsFail && testOperation.FailedTests > 0)
            {
                logger.Log($"Skipping coverage due to failed tests.  Option {nameof(AppOptions.RunWhenTestsFail)} is false");
                return;
            }

            var totalTests = testOperation.TotalTests;
            var runWhenTestsExceed = settings.RunWhenTestsExceed;
            if (totalTests > 0) // in case this changes to not reporting total tests
            {
                if (totalTests <= runWhenTestsExceed)
                {
                    logger.Log($"Skipping coverage as total tests ({totalTests}) <= {nameof(AppOptions.RunWhenTestsExceed)} ({runWhenTestsExceed})");
                    return;
                }
            }
            fccEngine.ReloadCoverage(testOperation.GetCoverageProjectsAsync);
        }

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            try
            {
                if(e.State == TestOperationStates.TestExecutionCanceling)
                {
                    fccEngine.StopCoverage();
                }

                
                if (e.State == TestOperationStates.TestExecutionStarting)
                {
                    TestExecutionStarting(e.Operation);
                }

                if (e.State == TestOperationStates.TestExecutionFinished)
                {
                    TestExecutionFinished(e.Operation);
                }
            }
            catch (Exception exception)
            {
                logger.Log("Error processing unit test events", exception);
            }
        }
    }
}
