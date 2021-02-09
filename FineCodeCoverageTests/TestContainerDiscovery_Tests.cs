using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class TestContainerDiscovery_Tests
    {
        private AutoMoqer mocker;
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

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            var testContainerDiscoverer = mocker.Create<TestContainerDiscoverer>();
            testContainerDiscoverer.initializeThread.Join();
        }

        [Test]
        public void It_Should_Initialize_As_Is_The_Entrance()
        {
            var serviceProvider = mocker.GetMock<IServiceProvider>();
            mocker.Verify<IInitializer>(i => i.Initialize(serviceProvider.Object));
        }

        [Test]
        public void Should_Stop_Coverage_When_Tests_Are_Cancelled()
        {
            RaiseTestExecutionCancelling();
            mocker.Verify<IFCCEngine>(e => e.StopCoverage());
        }
        private void Should_TryReloadCoverage_When_OperationStateChanged(TestOperationStates testOperationStates)
        {
            RaiseOperationStateChanged(testOperationStates);
            mocker.Verify<IFCCEngine>(e => e.TryReloadCoverage(It.IsAny<Func<IAppOptions, Task<ReloadCoverageRequest>>>()));
        }

        [Test]
        public void Should_TryReloadCoverage_When_TestExecutionStarting()
        {
            Should_TryReloadCoverage_When_OperationStateChanged(TestOperationStates.TestExecutionStarting);
        }

        [Test]
        public void Should_TryReloadCoverage_When_TestExecutionFinished()
        {
            Should_TryReloadCoverage_When_OperationStateChanged(TestOperationStates.TestExecutionFinished);
        }

        private Task<ReloadCoverageRequest> ReloadCoverageRequest(bool testExecutionStarting,Action<Mock<IAppOptions>> setUpAppOptions,IOperation operation = null)
        {
            var mockAppOptions = new Mock<IAppOptions>();
            setUpAppOptions(mockAppOptions);
            Task<ReloadCoverageRequest> reloadCoverageRequestTask = null;
            mocker.GetMock<IFCCEngine>().Setup(engine => engine.TryReloadCoverage(It.IsAny<Func<IAppOptions, Task<ReloadCoverageRequest>>>())).
                Callback<Func<IAppOptions, Task<ReloadCoverageRequest>>>(callback =>
                {
                    reloadCoverageRequestTask = callback(mockAppOptions.Object);
                });

            if (testExecutionStarting)
            {
                RaiseTestExecutionStarting(operation);
            }
            else
            {
                RaiseTestExecutionFinished(operation);
            }
            
            return reloadCoverageRequestTask;
        }

        [Test]
        public async Task Should_Not_Run_Coverage_When_TestExecutionStarting_If_Settings_RunInParallel_Is_False()
        {
            var reloadCoverageRequest = await ReloadCoverageRequest(true, mockAppOptions => mockAppOptions.Setup(o => o.RunInParallel).Returns(false));
            Assert.IsFalse(reloadCoverageRequest.Proceed);
        }

        [Test]
        public async Task Should_Not_Run_Coverage_When_TestExecutionFinished_If_Ran_When_TestExecutionStarting()
        {
            var reloadCoverageRequest = await ReloadCoverageRequest(false, mockAppOptions => mockAppOptions.Setup(o => o.RunInParallel).Returns(true));
            Assert.IsFalse(reloadCoverageRequest.Proceed);
        }

        private (IOperation operation,List<CoverageProject> coverageProjects,Mock<ITestOperation> mockTestOperation) SetUpForProceedPath()
        {
            var operation = new Mock<IOperation>().Object;
            var mockTestOperation = new Mock<ITestOperation>();
            var coverageProjects = new List<CoverageProject>();
            mockTestOperation.Setup(t => t.GetCoverageProjectsAsync()).Returns(Task.FromResult(coverageProjects));
            mocker.GetMock<ITestOperationFactory>().Setup(f => f.Create(operation)).Returns(mockTestOperation.Object);
            return (operation, coverageProjects, mockTestOperation);

        }

        [Test]
        public async Task Should_Run_Coverage_When_TestExecutionStarting_If_Settings_RunInParallel_Is_True()
        {
            var (operation,coverageProjects,_)= SetUpForProceedPath();
            var reloadCoverageRequest = await ReloadCoverageRequest(true, mockAppOptions => mockAppOptions.Setup(o => o.RunInParallel).Returns(true),operation);
            Assert.IsTrue(reloadCoverageRequest.Proceed);
            Assert.AreSame(coverageProjects, reloadCoverageRequest.CoverageProjects);
        }

        [TestCase(true, 10, 1, 0, true,Description ="Should run when tests fail if settings RunWhenTestsFail is true")]
        [TestCase(false, 10, 1, 0, false, Description = "Should not run when tests fail if settings RunWhenTestsFail is false")]
        [TestCase(false, 0, 1, 1, false, Description = "Should not run when total tests does not exceed the RunWhenTestsExceed setting")]
        [TestCase(false, 0, 1, 0, true, Description = "Should run when total tests does not exceed the RunWhenTestsExceed setting")]
        public async Task Conditional_Run_Coverage_When_TestExecutionFinished(bool runWhenTestsFail,long numberFailedTests,long totalTests, int runWhenTestsExceed,bool expectProceed)
        {
            var (operation, coverageProjects,mockTestOperation) = SetUpForProceedPath();
            mockTestOperation.Setup(o => o.FailedTests).Returns(numberFailedTests);
            mockTestOperation.Setup(o => o.TotalTests).Returns(totalTests);
            var reloadCoverageRequest = await ReloadCoverageRequest(false, mockAppOptions =>
            {
                mockAppOptions.Setup(o => o.RunInParallel).Returns(false);
                mockAppOptions.Setup(o => o.RunWhenTestsFail).Returns(runWhenTestsFail);
                mockAppOptions.Setup(o => o.RunWhenTestsExceed).Returns(runWhenTestsExceed);
            }, operation);

            if (expectProceed)
            {
                Assert.IsTrue(reloadCoverageRequest.Proceed);
                Assert.AreSame(coverageProjects, reloadCoverageRequest.CoverageProjects);
            }
            else
            {
                Assert.IsFalse(reloadCoverageRequest.Proceed);
            }
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