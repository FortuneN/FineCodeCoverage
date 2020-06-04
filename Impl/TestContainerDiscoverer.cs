using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(TestContainerDiscoverer))]
	[Export(typeof(ITestContainerDiscoverer))]
	[Name(ProjectMetaData.TestContainerDiscovererName)]
	internal class TestContainerDiscoverer : ITestContainerDiscoverer
	{
		public event EventHandler TestContainersUpdated;

		public Uri ExecutorUri => new Uri($"executor://{ProjectMetaData.Id}.Executor/v1");

		public IEnumerable<ITestContainer> TestContainers => Enumerable.Empty<ITestContainer>();

		[ImportingConstructor]
		internal TestContainerDiscoverer
		(
			[Import(typeof(IOperationState))]
			IOperationState operationState,

			[Import(typeof(SVsServiceProvider))]
			IServiceProvider serviceProvider
		)
		{
			TestContainersUpdated?.ToString();
			Logger.Initialize(serviceProvider, ProjectMetaData.Name);
			operationState.StateChanged += OperationState_StateChanged;
		}

		private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
		{
			try
			{
				if (e.State == TestOperationStates.TestExecutionFinished)
				{
					var operationType = e.Operation.GetType();
					var testConfiguration = (operationType.GetProperty("Configuration") ?? operationType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(e.Operation);
					var testConfigurationSources = (IEnumerable<string>)testConfiguration.GetType().GetProperty("TestSources").GetValue(testConfiguration);

					foreach (var testDllFile in testConfigurationSources)
					{
						CoverageUtil.LoadCoverageFromTestDllFile(testDllFile, exception =>
						{
							if (exception != null)
							{
								Logger.Log(exception);
								return;
							}

							TaggerProvider.ReloadTags();
						});
					}
				}
			}
			catch (Exception exception)
			{
				Logger.Log(exception);
			}
		}
	}
}
