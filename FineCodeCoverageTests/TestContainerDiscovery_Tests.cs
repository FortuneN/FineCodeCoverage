using System;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine;
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
            mocker.GetMock<IOperationState>().Raise(s => s.StateChanged += null, new OperationStateChangedEventArgs(TestOperationStates.TestExecutionCanceling));
            mocker.Verify<IFCCEngine>(e => e.StopCoverage());
        }
        private void Should_TryReloadCoverage_When_OperationStateChanged(TestOperationStates testOperationStates)
        {
            mocker.GetMock<IOperationState>().Raise(s => s.StateChanged += null, new OperationStateChangedEventArgs(testOperationStates));
            mocker.Verify<IFCCEngine>(e => e.TryReloadCoverage(It.IsAny<Func<AppOptions, Task<ReloadCoverageRequest>>>()));
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


    }
}