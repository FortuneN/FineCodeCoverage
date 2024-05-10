using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using NUnit.Framework;

namespace Test
{

    internal class TestOperationStateInvocationManager_Tests
    {
        private AutoMoqer mocker;
        private TestOperationStateInvocationManager testOperationStateInvocationManager;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            testOperationStateInvocationManager = mocker.Create<TestOperationStateInvocationManager>();
        }

        [Test]
        public void Should_Return_True_When_Initialized_And_TestExecutionStarting()
        {
            mocker.GetMock<IInitializeStatusProvider>().Setup(initializeStatusProvider => initializeStatusProvider.InitializeStatus).Returns(InitializeStatus.Initialized);
            Assert.That(testOperationStateInvocationManager.CanInvoke(TestOperationStates.TestExecutionStarting), Is.True);
        }

        [Test]
        public void Should_Return_False_When_Not_Initialized_And_TestExecutionStarting()
        {
            mocker.GetMock<IInitializeStatusProvider>().Setup(initializeStatusProvider => initializeStatusProvider.InitializeStatus).Returns(InitializeStatus.Initializing);
            Assert.That(testOperationStateInvocationManager.CanInvoke(TestOperationStates.TestExecutionStarting), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_True_For_All_Other_States_If_Was_Initialized_When_TestExecutionStarting(bool initializedWhenStarting)
        {
            var startingInitializeStatus = initializedWhenStarting ? InitializeStatus.Initialized : InitializeStatus.Initializing;
            mocker.GetMock<IInitializeStatusProvider>().Setup(initializeStatusProvider => initializeStatusProvider.InitializeStatus).Returns(startingInitializeStatus);
            testOperationStateInvocationManager.CanInvoke(TestOperationStates.TestExecutionStarting);
            Assert.That(testOperationStateInvocationManager.CanInvoke(TestOperationStates.TestExecutionCancelAndFinished), Is.EqualTo(initializedWhenStarting));
        }

        [TestCase(TestOperationStates.TestExecutionStarting)]
        [TestCase(TestOperationStates.TestExecutionFinished)]
        public void Should_Log_When_Cannot_Invoke(TestOperationStates testOperationState)
        {
            testOperationStateInvocationManager.CanInvoke(testOperationState);
            mocker.Verify<ILogger>(logger => logger.Log($"Skipping {testOperationState} as FCC not initialized"));
        }
       
    }

    internal class TestContainerDiscovery_Tests
    {
        private AutoMoqer mocker;
        private TestContainerDiscoverer testContainerDiscoverer;

        private void RaiseOperationStateChanged(TestOperationStates testOperationStates,IOperation operation = null)
        {
            var args = operation == null ? new OperationStateChangedEventArgs(testOperationStates) : new OperationStateChangedEventArgs(operation, (RequestStates)testOperationStates);
            mocker.GetMock<IOperationState>().Raise(s => s.StateChanged += null, args);
        }
        
        private void RaiseTestExecutionStarting(IOperation operation = null)
        {
            RaiseOperationStateChanged(TestOperationStates.TestExecutionStarting,operation);
        }

        private void RaiseTestExecutionFinished(IOperation operation = null)
        {
            RaiseOperationStateChanged(TestOperationStates.TestExecutionFinished,operation);
        }

        private void RaiseTestExecutionCancelling()
        {
            RaiseOperationStateChanged(TestOperationStates.TestExecutionCanceling);
        }

        private void AssertShouldNotReloadCoverage()
        {
            mocker.Verify<IFCCEngine>(engine => engine.ReloadCoverage(It.IsAny<Func<Task<List<ICoverageProject>>>>()), Times.Never());
        }

        private void AssertReloadsCoverage()
        {
            mocker.Verify<IFCCEngine>(engine => engine.ReloadCoverage(It.IsAny<Func<Task<List<ICoverageProject>>>>()), Times.Once());
        }

        private void SetUpOptions(Action<Mock<IAppOptions>> setupAppOptions)
        {
            var mockAppOptions = new Mock<IAppOptions>();
            setupAppOptions(mockAppOptions);
            mocker.GetMock<IAppOptionsProvider>().Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);
        }

        private (IOperation operation, List<ICoverageProject> coverageProjects, Mock<ITestOperation> mockTestOperation) SetUpForProceedPath()
        {
            var operation = new Mock<IOperation>().Object;
            var mockTestOperation = new Mock<ITestOperation>();
            var coverageProjects = new List<ICoverageProject>();
            mockTestOperation.Setup(t => t.GetCoverageProjectsAsync()).Returns(Task.FromResult(coverageProjects));
            mocker.GetMock<ITestOperationFactory>().Setup(f => f.Create(operation)).Returns(mockTestOperation.Object);
            return (operation, coverageProjects, mockTestOperation);

        }

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            testContainerDiscoverer = mocker.Create<TestContainerDiscoverer>();
            testContainerDiscoverer.RunAsync = (taskProvider) =>
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                taskProvider().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            };
            var mockTestOperationStateInvocationManager = mocker.GetMock<ITestOperationStateInvocationManager>();
            mockTestOperationStateInvocationManager.Setup(testOperationStateInvocationManager => testOperationStateInvocationManager.CanInvoke(It.IsAny<TestOperationStates>())).Returns(true);
        }

        [Test]
        public void It_Should_Load_The_Package()
        {
            mocker.Verify<IPackageLoader>(packageLoader => packageLoader.LoadPackageAsync(It.IsAny<CancellationToken>()));
        }

        [Test]
        public void Should_Stop_Coverage_When_Tests_Are_Cancelled()
        {
            RaiseTestExecutionCancelling();
            mocker.Verify<IFCCEngine>(e => e.StopCoverage());
        }

        [Test]
        public void Should_StopCoverage_When_TestExecutionStarting()
        {
            RaiseTestExecutionStarting();
            mocker.Verify<IFCCEngine>(engine => engine.StopCoverage());
        }

        [Test]
        public void Should_Stop_Ms_CodeCoverage_When_TestExecutionStarting_And_Ms_Code_Coverage_Collecting()
        {
            var mockMsCodeCoverageRunSettingsService = SetMsCodeCoverageCollecting();
            mockMsCodeCoverageRunSettingsService.Verify(
                msCodeCoverageRunSettingsService => msCodeCoverageRunSettingsService.StopCoverage(),
                Times.Never
            );

            RaiseTestExecutionStarting();

            mockMsCodeCoverageRunSettingsService.Verify(
                msCodeCoverageRunSettingsService => msCodeCoverageRunSettingsService.StopCoverage()
            );
        }

        [TestCase(MsCodeCoverageCollectionStatus.Collecting,true)]
        [TestCase(MsCodeCoverageCollectionStatus.NotCollecting,true)]
        [TestCase(MsCodeCoverageCollectionStatus.Error,true)]
        [TestCase(MsCodeCoverageCollectionStatus.Collecting, false)]
        [TestCase(MsCodeCoverageCollectionStatus.NotCollecting, false)]
        [TestCase(MsCodeCoverageCollectionStatus.Error, false)]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public void Should_Notify_MsCodeCoverage_When_Test_Execution_Not_Finished_IfCollectingAsync(MsCodeCoverageCollectionStatus status, bool cancelling)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var mockMsCodeCoverageRunSettingsService = SetMsCodeCoverageCollecting(status);
            var operation = new Mock<IOperation>().Object;
            var mockTestOperationFactory = mocker.GetMock<ITestOperationFactory>();
            var testOperation = new Mock<ITestOperation>().Object;
            mockTestOperationFactory.Setup(testOperationFactory => testOperationFactory.Create(operation)).Returns(testOperation);

            RaiseOperationStateChanged(
                cancelling ? TestOperationStates.TestExecutionCanceling : TestOperationStates.TestExecutionCancelAndFinished, 
                operation
            );
            var times = status == MsCodeCoverageCollectionStatus.Collecting ? Times.Once() : Times.Never();
            mockMsCodeCoverageRunSettingsService.Verify(
                msCodeCoverageRunSettingsService => msCodeCoverageRunSettingsService.TestExecutionNotFinishedAsync(testOperation), times
            );
        }

        private Mock<IMsCodeCoverageRunSettingsService> SetMsCodeCoverageCollecting(MsCodeCoverageCollectionStatus status = MsCodeCoverageCollectionStatus.Collecting)
        {
            var mockMsCodeCoverageRunSettingsService = mocker.GetMock<IMsCodeCoverageRunSettingsService>();
            mockMsCodeCoverageRunSettingsService.Setup(
                msCodeCoverageRunSettingsService =>
                msCodeCoverageRunSettingsService.IsCollectingAsync(It.IsAny<ITestOperation>())
            ).ReturnsAsync(status);

            SetUpOptions(mockOptions => mockOptions.Setup(options => options.Enabled).Returns(true));
            RaiseTestExecutionStarting();
            return mockMsCodeCoverageRunSettingsService;
        }


        [Test]
        public void Should_Not_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_False()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(true);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(false);
            });
            RaiseTestExecutionStarting();

            AssertShouldNotReloadCoverage();
        }

        [Test]
        public void Should_Not_ReloadCoverage_When_TestExecutionFinished_And_Reloading_When_Tests_Start()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(true);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
            });
            RaiseTestExecutionFinished();

            AssertShouldNotReloadCoverage();
        }

        [Test]
        public void Should_ReloadCoverage_When_TestExecutionFinished_If_RunInParallel_And_Not_Collecting_With_MsCodeCoverage()
        {
            var (operation,_,__) = SetUpForProceedPath();
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(true);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
                mockAppOptions.Setup(o => o.RunMsCodeCoverage).Returns(RunMsCodeCoverage.Yes);
            });
            RaiseTestExecutionFinished(operation);

            mocker.Verify<IFCCEngine>(engine => engine.ReloadCoverage(It.IsAny<Func<Task<List<ICoverageProject>>>>()));
        }

        [Test]
        public void Should_Collect_Ms_Code_Coverage_When_TestExecutionFinished_And_Ms_Code_Coverage_Collecting()
        {
            SetMsCodeCoverageCollecting();

            var operation = new Mock<IOperation>().Object;
            var testOperation = new Mock<ITestOperation>().Object;
            var mockTestOperationFactory = mocker.GetMock<ITestOperationFactory>();
            mockTestOperationFactory.Setup(testOperationFactory => testOperationFactory.Create(operation)).Returns(testOperation);

            RaiseTestExecutionFinished(operation);
            mocker.Verify<IMsCodeCoverageRunSettingsService>(
                msCodeCoverageRunSettingsService =>
                msCodeCoverageRunSettingsService.CollectAsync(operation,testOperation)
            );
        }

        [Test]
        public async Task Should_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_True_Async()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(true);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
            });
            var (operation, coverageProjects, mockTestOperation) = SetUpForProceedPath();
            Task<List<ICoverageProject>> reloadCoverageTask = null;
            mocker.GetMock<IFCCEngine>().Setup(engine => engine.ReloadCoverage(It.IsAny<Func<Task<List<ICoverageProject>>>>())).
                Callback<Func<Task<List<ICoverageProject>>>>(callback =>
                {
                    reloadCoverageTask = callback();
                });
            RaiseTestExecutionStarting(operation);
            Assert.AreSame(coverageProjects, await reloadCoverageTask);
        }

        [Test]
        public void Should_Not_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_True_When_Enabled_Is_False_And_DisabledNoCoverage_True()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(false);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
                mockAppOptions.Setup(o => o.DisabledNoCoverage).Returns(true);
            });
            
            RaiseTestExecutionStarting();
            AssertShouldNotReloadCoverage();
        }

        [Test]
        public void Should_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_True_When_Enabled_Is_False_And_DisabledNoCoverage_False()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(false);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
                mockAppOptions.Setup(o => o.DisabledNoCoverage).Returns(false);
            });

            RaiseTestExecutionStarting();
            AssertReloadsCoverage();
        }

        [TestCase(true, 10, 1, 0, true, Description = "Should run when tests fail if settings RunWhenTestsFail is true")]
        [TestCase(false, 10, 1, 0, false, Description = "Should not run when tests fail if settings RunWhenTestsFail is false")]
        [TestCase(false, 0, 1, 1, false, Description = "Should not run when total tests does not exceed the RunWhenTestsExceed setting")]
        [TestCase(false, 0, 1, 0, true, Description = "Should run when total tests does not exceed the RunWhenTestsExceed setting")]
        public async Task Conditional_Run_Coverage_When_TestExecutionFinished_Async(bool runWhenTestsFail, long numberFailedTests, long totalTests, int runWhenTestsExceed, bool expectReloadedCoverage)
        {
            var (operation, coverageProjects, mockTestOperation) = SetUpForProceedPath();
            mockTestOperation.Setup(o => o.FailedTests).Returns(numberFailedTests);
            mockTestOperation.Setup(o => o.TotalTests).Returns(totalTests);
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(true);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(false);
                mockAppOptions.Setup(o => o.RunWhenTestsFail).Returns(runWhenTestsFail);
                mockAppOptions.Setup(o => o.RunWhenTestsExceed).Returns(runWhenTestsExceed);
            });
            Task<List<ICoverageProject>> reloadCoverageTask = null;
            mocker.GetMock<IFCCEngine>().Setup(engine => engine.ReloadCoverage(It.IsAny<Func<Task<List<ICoverageProject>>>>())).
                Callback<Func<Task<List<ICoverageProject>>>>(callback =>
                {
                    reloadCoverageTask = callback();
                });
            RaiseTestExecutionFinished(operation);

            if (expectReloadedCoverage)
            {
                Assert.AreSame(coverageProjects, await reloadCoverageTask);
            }
            else
            {
                AssertShouldNotReloadCoverage();
            }
            
        }

        [Test]
        public void Should_Not_Run_Coverage_When_TestExecutionFinished_If_Enabled_Is_False()
        {
            var operation = new Mock<IOperation>().Object;
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(t => t.TotalTests).Returns(1);
            mocker.GetMock<ITestOperationFactory>().Setup(f => f.Create(operation)).Returns(mockTestOperation.Object);

            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(false);

                mockAppOptions.Setup(o => o.RunWhenTestsFail).Returns(true);
                mockAppOptions.Setup(o => o.RunWhenTestsExceed).Returns(0);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(false);
            });
            RaiseTestExecutionFinished();

            AssertShouldNotReloadCoverage();
        }

        [Test]
        public void Should_Handle_Any_Exception_In_OperationState_Changed_Handler_Logging_The_Exception()
        {
            var exception = new Exception();
            mocker.GetMock<IFCCEngine>().Setup(engine => engine.StopCoverage()).Throws(exception);
            RaiseTestExecutionCancelling();
            mocker.Verify<ILogger>(logger => logger.Log("Error processing unit test events", exception));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Not_Handle_OperationState_Changes_When_The_testOperationStateInvocationManager_Cannot_Invoke(bool canInvoke)
        {
            var invoked = false;
            testContainerDiscoverer.testOperationStateChangeHandlers = new Dictionary<TestOperationStates, Func<IOperation, Task>>
            {
                {TestOperationStates.TestExecutionCanceling, (_) => {invoked = true; return Task.CompletedTask; } }
            };
            var mockTestOperationStateInvocationManager = mocker.GetMock<ITestOperationStateInvocationManager>();
            mockTestOperationStateInvocationManager.Setup(testOperationStateInvocationManager => testOperationStateInvocationManager.CanInvoke(It.IsAny<TestOperationStates>())).Returns(canInvoke);
           
            RaiseTestExecutionCancelling();
            Assert.That(invoked, Is.EqualTo(canInvoke));
        }

        [Test]
        public void Should_Send_TestExecutionStartingMessage_When_TestExecutionStarting()
        {
            var operation = new Mock<IOperation>().Object;
            RaiseTestExecutionStarting(operation);
            mocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(It.IsAny<TestExecutionStartingMessage>(),null));
        }
    }
}