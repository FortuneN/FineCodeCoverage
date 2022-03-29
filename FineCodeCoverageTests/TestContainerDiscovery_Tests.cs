using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
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
            var mockDisposeAwareTaskRunner = mocker.GetMock<IDisposeAwareTaskRunner>();
            mockDisposeAwareTaskRunner.Setup(runner => runner.RunAsync(It.IsAny<Func<Task>>())).Callback<Func<Task>>(async taskProvider => await taskProvider());
            testContainerDiscoverer = mocker.Create<TestContainerDiscoverer>();
            testContainerDiscoverer.RunAsync = (taskProvider) =>
            {
                taskProvider().Wait();
            };
            testContainerDiscoverer.initializeTask.Wait();
        }

        [Test]
        public void It_Should_Initialize_As_Is_The_Entrance()
        {
            mocker.Verify<IInitializer>(i => i.InitializeAsync(It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task It_Should_Watch_For_Operation_State_Change_Before_Initialize()
        {
            List<int> order = new List<int>();
            mocker = new AutoMoqer();
            var mockDisposeAwareTaskRunner = mocker.GetMock<IDisposeAwareTaskRunner>();
            mockDisposeAwareTaskRunner.Setup(runner => runner.RunAsync(It.IsAny<Func<Task>>())).Callback<Func<Task>>(async taskProvider => await taskProvider());
            var mockOperationState = mocker.GetMock<IOperationState>();
            mockOperationState.SetupAdd(o => o.StateChanged += It.IsAny<EventHandler<OperationStateChangedEventArgs>>()).Callback(() =>
            {
                order.Add(1);
            });
            var mockInitializer = mocker.GetMock<IInitializer>();
            mockInitializer.Setup(i => i.InitializeAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                order.Add(2);
            });
            var testContainerDiscoverer = mocker.Create<TestContainerDiscoverer>();
            await testContainerDiscoverer.initializeTask;
            Assert.AreEqual(new List<int> { 1, 2 }, order);
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
        public void Should_Notify_MsCodeCoverage_When_Test_Execution_Not_Finished_IfCollectingAsync(MsCodeCoverageCollectionStatus status, bool cancelling)
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
        public async Task Should_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_True()
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
        public void Should_Not_ReloadCoverage_When_TestExecutionStarting_And_Settings_RunInParallel_Is_True_When_Enabled_is_False()
        {
            SetUpOptions(mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.Enabled).Returns(false);
                mockAppOptions.Setup(o => o.RunInParallel).Returns(true);
            });
            
            RaiseTestExecutionStarting();
            AssertShouldNotReloadCoverage();
        }

        [TestCase(true, 10, 1, 0, true, Description = "Should run when tests fail if settings RunWhenTestsFail is true")]
        [TestCase(false, 10, 1, 0, false, Description = "Should not run when tests fail if settings RunWhenTestsFail is false")]
        [TestCase(false, 0, 1, 1, false, Description = "Should not run when total tests does not exceed the RunWhenTestsExceed setting")]
        [TestCase(false, 0, 1, 0, true, Description = "Should run when total tests does not exceed the RunWhenTestsExceed setting")]
        public async Task Conditional_Run_Coverage_When_TestExecutionFinished(bool runWhenTestsFail, long numberFailedTests, long totalTests, int runWhenTestsExceed, bool expectReloadedCoverage)
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
    }
}