using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using NUnit.Framework;

namespace Test
{
    public class Initializer_Tests
    {
        private AutoMoqer mocker;
        private Initializer initializer;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            initializer = mocker.Create<Initializer>();
        }

		[Test]
		public void Should_Have_Initial_InitializeStatus_As_Initializing()
        {
			Assert.AreEqual(InitializeStatus.Initializing, initializer.InitializeStatus);
        }

		[Test]
		public async Task Should_Log_Initializing_When_Initialize()
        {
			await initializer.InitializeAsync(CancellationToken.None);
			mocker.Verify<ILogger>(l => l.Log("Initializing"));
        }

		private async Task InitializeWithExceptionAsync(Action<Exception> callback = null)
		{
			var initializeException = new Exception("initialize exception");
			mocker.Setup<ICoverageProjectFactory>(a => a.Initialize()).Throws(initializeException);
			
			await initializer.InitializeAsync(CancellationToken.None);
			callback?.Invoke(initializeException);

		}
		[Test]
		public async Task Should_Set_InitializeStatus_To_Error_If_Exception_When_Initialize()
		{
			await InitializeWithExceptionAsync();
			Assert.AreEqual(InitializeStatus.Error, initializer.InitializeStatus);
		}

		[Test]
		public async Task Should_Set_InitializeExceptionMessage_If_Exception_When_Initialize()
		{
			await InitializeWithExceptionAsync();
			Assert.AreEqual("initialize exception", initializer.InitializeExceptionMessage);
		}

		[Test]
		public async Task Should_Log_Failed_Initialization_With_Exception_if_Exception_When_Initialize()
        {
			Exception initializeException = null;
			await InitializeWithExceptionAsync(exc => initializeException = exc);
			mocker.Verify<ILogger>(l => l.Log("Failed Initialization", initializeException));
		}

		[Test]
		public async Task Should_Set_InitializeStatus_To_Initialized_When_Successfully_Completed()
		{
			await initializer.InitializeAsync(CancellationToken.None);
			Assert.AreEqual(InitializeStatus.Initialized, initializer.InitializeStatus);
		}

		[Test]
		public async Task Should_Log_Initialized_When_Successfully_Completed()
		{
			await initializer.InitializeAsync(CancellationToken.None);
			mocker.Verify<ILogger>(l => l.Log("Initialized"));
		}

		[Test]
		public async Task Should_Initialize_Dependencies_In_Order()
        {
			var disposalToken = CancellationToken.None;
			List<int> callOrder = new List<int>();
			mocker.GetMock<ICoverageProjectFactory>().Setup(cp => cp.Initialize()).Callback(() =>
			{
				callOrder.Add(1);
			});
			mocker.GetMock<IFCCEngine>().Setup(engine => engine.Initialize(initializer, disposalToken)).Callback(() =>
			{
				callOrder.Add(2);
			});

			mocker.GetMock<IPackageInitializer>().Setup(p => p.InitializeAsync(disposalToken)).Callback(() =>
			{
				callOrder.Add(3);
			});

			await initializer.InitializeAsync(disposalToken);
			Assert.AreEqual(new List<int> { 1, 2, 3 }, callOrder);
		}

		[Test]
		public async Task Should_Pass_Itself_To_FCCEngine_For_InitializeStatus()
        {
			var disposalToken = CancellationToken.None;
			await initializer.InitializeAsync(disposalToken);
			mocker.Verify<IFCCEngine>(engine => engine.Initialize(initializer, disposalToken));
        }

	}
}