using System;
using System.IO;
using AutoMoqCore;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class TestContainerDiscovery_Tests
    {
        [Test]
        public void It_Should_Initialize_As_Is_The_Entrance()
        {

            var mockedServiceProvider = new Mock<IServiceProvider>().Object;
            var mockInitializer = new Mock<IInitializer>();
            var testContainerDiscoverer = new TestContainerDiscoverer(new Mock<IOperationState>().Object, mockedServiceProvider, null, null, null, mockInitializer.Object);
            testContainerDiscoverer.initializeThread.Join();
            mockInitializer.Verify(i => i.Initialize(mockedServiceProvider));

            /*
                Would like to do below but getting exception

                Unity.Exceptions.ResolutionFailedException : Resolution of the dependency failed, type = 'FineCodeCoverage.Impl.TestContainerDiscoverer', name = '(none)'.
                Exception occurred while: while resolving.
                Exception is: FileNotFoundException - Could not load file or assembly 'System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. The system cannot find the file specified.

                var mocker = new AutoMoqer();
                var testContainerDiscoverer = mocker.Create<TestContainerDiscoverer>();
                testContainerDiscoverer.initializeThread.Join();
                var serviceProvider = mocker.GetMock<IServiceProvider>();
                mocker.Verify<IInitializer>(i => i.Initialize(serviceProvider.Object));
            */

        }

        [Test]
        public void Should_Stop_Coverage_When_Tests_Are_Cancelled()
        {
            var mockedServiceProvider = new Mock<IServiceProvider>().Object;
            var mockInitializer = new Mock<IInitializer>();
            var mockOperationState = new Mock<IOperationState>();
            var mockFCCEngine = new Mock<IFCCEngine>();
            var testContainerDiscoverer = new TestContainerDiscoverer(mockOperationState.Object, mockedServiceProvider, mockFCCEngine.Object, null, null, mockInitializer.Object);
            testContainerDiscoverer.initializeThread.Join();
            mockOperationState.Raise(s => s.StateChanged += null, new OperationStateChangedEventArgs(TestOperationStates.Canceling));
            mockFCCEngine.Verify(e => e.StopCoverage());
        }


        [Test]
        public void Should_TryReloadCoverage_When_Tests_Start_But_Return_Null_If_Settings_Are_Not_RunInParallel()
        {

        }

        [Test]
        public void Should_TryReloadCoverage_When_Tests_Start_Returning_CoverageProjects_When_Settings_RunInParallel()
        {
            //Operation factory - create interface so do not need to go through reflectobjectproperties.
        }


    }
}