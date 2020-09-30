using System;
using System.Xml;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.IO;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Impl
{
	[Export(typeof(TestContainerDiscoverer))]
	[Export(typeof(ITestContainerDiscoverer))]
	[Name(Vsix.TestContainerDiscovererName)]
	internal class TestContainerDiscoverer : ITestContainerDiscoverer
	{
		public event EventHandler TestContainersUpdated;

		public Uri ExecutorUri => new Uri($"executor://{Vsix.Code}.Executor/v1");

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
			try
			{
				Logger.Clear();
				Logger.Initialize(serviceProvider, Vsix.Name);

				CoverageUtil.Initialize();
				LoadToolWindow(serviceProvider);

				TestContainersUpdated?.ToString();
				operationState.StateChanged += OperationState_StateChanged;

				Logger.Log
				(
					"Initialized",
					$"Version                   {GetVersion()}",
					$"Coverlet Version          {CoverageUtil.CurrentCoverletVersion}",
					$"Report Generator Version  {CoverageUtil.CurrentReportGeneratorVersion}",
					$"Work Folder               {CoverageUtil.AppDataFolder}",
					$"Coverlet Folder           {CoverageUtil.AppDataCoverletFolder}",
					$"Report Generator Folder   {CoverageUtil.AppDataReportGeneratorFolder}"
				);
			}
			catch (Exception exception)
			{
				Logger.Log($"Failed Initialization", exception.ToString());
			}
		}

		[SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
		private void LoadToolWindow(IServiceProvider serviceProvider)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
				{
					var PackageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
					shell.LoadPackage(ref PackageToBeLoadedGuid, out var package);
					OutputToolWindowCommand.Instance.Execute(default, default);
				}
			});
		}

		private static string GetVersion()
		{
			Assembly assembly = null;

			try
			{
				var doc = new XmlDocument();
				assembly = typeof(TestContainerDiscoverer).Assembly;
				doc.Load(Path.Combine(Path.GetDirectoryName(assembly.Location), "extension.vsixmanifest"));
				var metaData = doc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Metadata");
				var identity = metaData.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Identity");
				var version = identity.GetAttribute("Version");
				return version;
			}
			catch
			{
				try
				{
					return assembly.GetName().Version.ToString();
				}
				catch
				{
					return "-";
				}
			}
		}

		private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
		{
			try
			{
				if (e.State == TestOperationStates.TestExecutionFinished)
				{
					Logger.Clear();
					Logger.Log("Updating coverage ...");

					var operationType = e.Operation.GetType();
					var testConfiguration = (operationType.GetProperty("Configuration") ?? operationType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(e.Operation);
					var testConfigurationSources = (IEnumerable<string>)testConfiguration.GetType().GetProperty("TestSources").GetValue(testConfiguration);

					foreach (var testDllFile in testConfigurationSources)
					{
						CoverageUtil.LoadCoverageFromTestDllFile(testDllFile, exception =>
						{
							if (exception != null)
							{
								Logger.Log("Error updating coverage", exception);
								return;
							}

							TaggerProvider.ReloadTags();
							
							OutputToolWindowControl.SetFilePaths
							(
								CoverageUtil.SummaryHtmlFilePath,
								CoverageUtil.CoverageHtmlFilePath,
								CoverageUtil.RiskHotspotsHtmlFilePath
							);

							Logger.Log("Coverage updated!");
						});
					}
				}
			}
			catch (Exception exception)
			{
				Logger.Log("Error processing unit test events", exception);
			}
		}
	}
}
