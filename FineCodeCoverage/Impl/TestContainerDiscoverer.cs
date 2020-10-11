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
using FineCodeCoverage.Options;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Engine.OpenCover;

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
				Logger.Initialize(serviceProvider);
				FCCEngine.Initialize();
				LoadToolWindow(serviceProvider);

				TestContainersUpdated?.ToString();
				operationState.StateChanged += OperationState_StateChanged;

				Logger.Log
				(
					"Initialized",
					$"Version                     {GetVersion()}",
					$"Work Folder                 {FCCEngine.AppDataFolder}",
					$"Coverlet Version            {CoverletUtil.CurrentCoverletVersion}",
					$"Coverlet Folder             {CoverletUtil.AppDataCoverletFolder}",
					$"Report Generator Version    {ReportGeneratorUtil.CurrentReportGeneratorVersion}",
					$"Report Generator Folder     {ReportGeneratorUtil.AppDataReportGeneratorFolder}",
					$"OpenCover Generator Version {OpenCoverUtil.CurrentOpenCoverVersion}",
					$"OpenCover Generator Folder  {OpenCoverUtil.AppDataOpenCoverFolder}"
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
					var settings = AppOptions.Get();

					if (!settings.Enabled)
					{
						FCCEngine.CoverageLines.Clear();
						TaggerProvider.ReloadTags();
						OutputToolWindowControl.SetFilePaths(default, default, default);
						return;
					}

					Logger.Log("================================== START ==================================");

					var operationType = e.Operation.GetType();
					var testConfiguration = (operationType.GetProperty("Configuration") ?? operationType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(e.Operation);
					var testDllFiles = ((IEnumerable<string>)testConfiguration.GetType().GetProperty("TestSources").GetValue(testConfiguration)).ToArray();

					FCCEngine.ReloadCoverage
					(
						testDllFiles,
						(error) =>
						{
							if (error != null)
							{
								Logger.Log("Margin Tags Error", error);
								return;
							}

							TaggerProvider.ReloadTags();
						},
						(error) =>
						{
							if (error != null)
							{
								Logger.Log("Output Window Error", error);
								return;
							}

							OutputToolWindowControl.SetFilePaths
							(
								FCCEngine.SummaryHtmlFilePath,
								FCCEngine.CoverageHtmlFilePath,
								FCCEngine.RiskHotspotsHtmlFilePath
							);
						},
						(error) =>
						{
							if (error != null)
							{
								Logger.Log("Error", error);
								return;
							}

							Logger.Log("================================== DONE ===================================");
						}
					);
				}
			}
			catch (Exception exception)
			{
				Logger.Log("Error processing unit test events", exception);
			}
		}
	}
}
