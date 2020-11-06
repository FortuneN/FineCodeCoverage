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
using FineCodeCoverage.Engine.Model;

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

		public static event UpdateMarginTagsDelegate UpdateMarginTags;
		public static event UpdateOutputWindowDelegate UpdateOutputWindow;

		public delegate void UpdateMarginTagsDelegate(object sender, UpdateMarginTagsEventArgs e);
		public delegate void UpdateOutputWindowDelegate(object sender, UpdateOutputWindowEventArgs e);

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
				InitializeOutputWindow(serviceProvider);

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
		private void InitializeOutputWindow(IServiceProvider serviceProvider)
		{
			// First time initialization -> So first time users don't have to dig through [View > Other Windows > FCC] which they won't know about

			var outputWindowInitializedFile = Path.Combine(FCCEngine.AppDataFolder, "outputWindowInitialized");

			if (!File.Exists(outputWindowInitializedFile))
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
					{
						var packageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
						shell.LoadPackage(ref packageToBeLoadedGuid, out var package);

						OutputToolWindowCommand.Instance.ShowToolWindow();
						File.WriteAllText(outputWindowInitializedFile, string.Empty);
					}
				});
			}
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
						UpdateMarginTags?.Invoke(this, null);
						UpdateOutputWindow?.Invoke(this, null);
						return;
					}

					Logger.Log("================================== START ==================================");

					var operationType = e.Operation.GetType();
					var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
					var testConfiguration = (operationType.GetProperty("Configuration") ?? operationType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(e.Operation);
					var testContainers = ((IEnumerable<object>) testConfiguration.GetType().GetProperty("Containers").GetValue(testConfiguration)).ToArray();
					var projects = new List<CoverageProject>(); 
					
					foreach (var container in testContainers)
					{
						var project = new CoverageProject();
						var containerType = container.GetType();
						var containerData = containerType.GetProperty("ProjectData").GetValue(container);
						var containerDataType = containerData.GetType();
						
						project.ProjectGuid = containerType.GetProperty("ProjectGuid").GetValue(container).ToString();
						project.ProjectName = containerType.GetProperty("ProjectName").GetValue(container).ToString();
						project.TestDllFileInOutputFolder = containerType.GetProperty("Source").GetValue(container).ToString();
						project.ProjectFile = containerDataType.GetProperty("ProjectFilePath", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic).GetValue(containerData).ToString();
						
						var defaultOutputFolder = Path.GetDirectoryName(containerDataType.GetProperty("DefaultOutputPath", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic).GetValue(containerData).ToString());
						project.WorkFolder = Path.Combine(Path.GetDirectoryName(defaultOutputFolder), "fine-code-coverage");

						projects.Add(project);
					}

					FCCEngine.ReloadCoverage
					(
						projects.ToArray(),
						darkMode,
						(error) =>
						{
							if (error != null)
							{
								Logger.Log("Margin Tags Error", error);
								return;
							}

							UpdateMarginTags?.Invoke(this, UpdateMarginTagsEventArgs.Empty);
						},
						(error) =>
						{
							if (error != null)
							{
								Logger.Log("Output Window Error", error);
								return;
							}

							UpdateOutputWindow?.Invoke(this, new UpdateOutputWindowEventArgs
							{
								HtmlContent = File.ReadAllText(FCCEngine.HtmlFilePath)
							});
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

	public class UpdateMarginTagsEventArgs : EventArgs
	{
		public static new readonly UpdateMarginTagsEventArgs Empty = new UpdateMarginTagsEventArgs(); 
	}

	public class UpdateOutputWindowEventArgs : EventArgs
	{
		public static new readonly UpdateOutputWindowEventArgs Empty = new UpdateOutputWindowEventArgs();

		public string HtmlContent { get; set; }
	}
}
