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
using System.Runtime.InteropServices;

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

		private readonly IServiceProvider _serviceProvider;

		private string CurrentTheme => $"{((dynamic)_serviceProvider.GetService(typeof(SVsColorThemeService)))?.CurrentTheme?.Name}".Trim();

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
				_serviceProvider = serviceProvider;

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

		[SuppressMessage("Usage", "VSTHRD108:Assert thread affinity unconditionally")]
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
						OutputToolWindowControl.Clear();
						return;
					}

					Logger.Log("================================== START ==================================");

					var operationType = e.Operation.GetType();
					var testConfiguration = (operationType.GetProperty("Configuration") ?? operationType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(e.Operation);
					var testDllFiles = ((IEnumerable<string>)testConfiguration.GetType().GetProperty("TestSources").GetValue(testConfiguration)).ToArray();
					var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

					FCCEngine.ReloadCoverage
					(
						testDllFiles,
						darkMode,
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

							OutputToolWindowControl.SetFilePath(FCCEngine.HtmlFilePath);
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

	[Guid("0D915B59-2ED7-472A-9DE8-9161737EA1C5")]
	[SuppressMessage("Style", "IDE1006:Naming Styles")]
	public interface SVsColorThemeService {}
}
