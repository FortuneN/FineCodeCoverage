using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Output;
using Moq;
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
			mocker.SetInstance(new IInitializable[] { });
            initializer = mocker.Create<Initializer>();
        }

		[Test]
		public void Should_ImportMany_IInitializable()
		{
			var constructor = typeof(Initializer).GetConstructors().Single();
			var initializablesConstructorParameter = constructor.GetParameters().Single(p => p.ParameterType == typeof(IInitializable[]));
			var hasImportManyAttribute = initializablesConstructorParameter.GetCustomAttributes(typeof(ImportManyAttribute), false).Any();
			Assert.True(hasImportManyAttribute);
        }

		[Test]
		public void Should_Have_Initial_InitializeStatus_As_Initializing()
        {
			Assert.AreEqual(InitializeStatus.Initializing, initializer.InitializeStatus);
        }

		[Test]
		public async Task Should_Log_Initializing_When_Initialize_Async()
        {
			await initializer.InitializeAsync(CancellationToken.None);
			mocker.Verify<ILogger>(l => l.Log("Initializing"));
        }

		private async Task InitializeWithExceptionAsync(Action<Exception> callback = null)
		{
			var initializeException = new Exception("initialize exception");
			mocker.Setup<IFCCEngine>(fccEngine => fccEngine.Initialize(It.IsAny<CancellationToken>())).Throws(initializeException);
			
			await initializer.InitializeAsync(CancellationToken.None);
			callback?.Invoke(initializeException);

		}
		[Test]
		public async Task Should_Set_InitializeStatus_To_Error_If_Exception_When_Initialize_Async()
		{
			await InitializeWithExceptionAsync();
			Assert.AreEqual(InitializeStatus.Error, initializer.InitializeStatus);
		}

		[Test]
		public async Task Should_Set_InitializeExceptionMessage_If_Exception_When_Initialize_Async()
		{
			await InitializeWithExceptionAsync();
			Assert.AreEqual("initialize exception", initializer.InitializeExceptionMessage);
		}

		[Test]
		public async Task Should_Log_Failed_Initialization_With_Exception_if_Exception_When_Initialize_Async()
        {
			Exception initializeException = null;
			await InitializeWithExceptionAsync(exc => initializeException = exc);
			mocker.Verify<ILogger>(l => l.Log("Failed Initialization", initializeException));
		}

		[Test]
		public async Task Should_Set_InitializeStatus_To_Initialized_When_Successfully_Completed_Async()
		{
			await initializer.InitializeAsync(CancellationToken.None);
			Assert.AreEqual(InitializeStatus.Initialized, initializer.InitializeStatus);
		}

		[Test]
		public async Task Should_Log_Initialized_When_Successfully_Completed_Async()
		{
			await initializer.InitializeAsync(CancellationToken.None);
			mocker.Verify<ILogger>(l => l.Log("Initialized"));
		}

		[Test]
		public async Task Should_Initialize_Dependencies_In_Order_Async()
        {
			var disposalToken = CancellationToken.None;
			List<int> callOrder = new List<int>();
			mocker.GetMock<IFCCEngine>().Setup(engine => engine.Initialize(disposalToken)).Callback(() =>
			{
				callOrder.Add(1);
			});

			mocker.GetMock<IFirstTimeToolWindowOpener>().Setup(firstTimeToolWindowOpener => firstTimeToolWindowOpener.OpenIfFirstTimeAsync(disposalToken)).Callback(() =>
			{
				callOrder.Add(2);
			});

			await initializer.InitializeAsync(disposalToken);
			Assert.AreEqual(new List<int> { 1, 2 }, callOrder);
		}
	}
}