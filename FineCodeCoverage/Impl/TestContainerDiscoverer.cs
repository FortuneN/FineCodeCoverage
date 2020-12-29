using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using FineCodeCoverage.Output;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using FineCodeCoverage.Engine.Model;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using FineCodeCoverage.Engine.Utilities;

namespace FineCodeCoverage.Impl
{
    [Name(Vsix.TestContainerDiscovererName)]
	[Export(typeof(TestContainerDiscoverer))]
	[Export(typeof(ITestContainerDiscoverer))]
	internal class TestContainerDiscoverer : ITestContainerDiscoverer
	{
		private EnvDTE.DTE _dte;
		private EnvDTE.Events _dteEvents;
		private Thread _reloadCoverageThread;
		private EnvDTE.BuildEvents _dteBuildEvents;
		public event EventHandler TestContainersUpdated;
		private readonly IServiceProvider _serviceProvider;
		public static event UpdateMarginTagsDelegate UpdateMarginTags;
		public static event UpdateOutputWindowDelegate UpdateOutputWindow;
		public Uri ExecutorUri => new Uri($"executor://{Vsix.Code}.Executor/v1");
		public IEnumerable<ITestContainer> TestContainers => Enumerable.Empty<ITestContainer>();
		public delegate void UpdateMarginTagsDelegate(object sender, UpdateMarginTagsEventArgs e);
		public delegate void UpdateOutputWindowDelegate(object sender, UpdateOutputWindowEventArgs e);
		private string CurrentTheme => $"{((dynamic)_serviceProvider.GetService(typeof(SVsColorThemeService)))?.CurrentTheme?.Name}".Trim();

		private ICoverletCoberturaCollector coverletCoberturaCollector;
		private ICoverletCoberturaCollectorFactory _coverletCoberturaCollectorFactory;
		[ImportingConstructor]
		internal TestContainerDiscoverer
		(
			[Import(typeof(IOperationState))]
			IOperationState operationState,

			[Import(typeof(SVsServiceProvider))]
			IServiceProvider serviceProvider,

            [Import(typeof(ICoverletCoberturaCollectorFactory))]
            ICoverletCoberturaCollectorFactory coverletCoberturaCollectorFactory
        )
		{
			_serviceProvider = serviceProvider;
			_coverletCoberturaCollectorFactory = coverletCoberturaCollectorFactory;
			new Thread(() =>
			{
				try
				{		
					Logger.Initialize(_serviceProvider);

					FCCEngine.Initialize();
					Initialize(_serviceProvider);
					TestContainersUpdated.ToString();
					operationState.StateChanged += OperationState_StateChanged;
					Logger.Log($"Initialized");
				}
				catch (Exception exception)
				{
					Logger.Log($"Failed Initialization", exception);
				}
			}).Start();
		}

		[SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
		private void Initialize(IServiceProvider serviceProvider)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));
				
				if (_dte != null)
				{
					_dteEvents = _dte.Events;
					_dteBuildEvents = _dteEvents.BuildEvents;
					_dteBuildEvents.OnBuildBegin += (scope, action) => StopCoverageProcess();
				}

				if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
				{
					var packageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
					shell.LoadPackage(ref packageToBeLoadedGuid, out var package);

					var outputWindowInitializedFile = Path.Combine(FCCEngine.AppDataFolder, "outputWindowInitialized");

					if (File.Exists(outputWindowInitializedFile))
					{
						OutputToolWindowCommand.Instance.FindToolWindow();
					}
					else
					{
						// for first time users, the window is automatically docked 
						OutputToolWindowCommand.Instance.ShowToolWindow();
						File.WriteAllText(outputWindowInitializedFile, string.Empty);
					}
				}
			});
		}
		private void DisposeCoberturaCollector()
        {
			if (coverletCoberturaCollector != null)
			{
				coverletCoberturaCollector.Dispose();
				coverletCoberturaCollector = null;
			}
		}
		private void StopCoverageProcess()
		{
			try
			{
				DisposeCoberturaCollector();
				_reloadCoverageThread?.Abort();
			}
			catch
			{
				// ignore
			}
			finally
			{
				_reloadCoverageThread = null;
				FCCEngine.ClearProcesses();
			}
		}

		[SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
		private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
		{
			try
			{
				if (e.State == TestOperationStates.TestExecutionStarting)
				{
					StopCoverageProcess(); // just to be sure

					coverletCoberturaCollector = _coverletCoberturaCollectorFactory.Create(e.Operation);

					
				}

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
					var userRunSettings = testConfiguration.GetType().GetProperty("UserRunSettings", BindingFlags.Instance | BindingFlags.Public).GetValue(testConfiguration);
					var runSettingsRetriever = new RunSettingsRetriever();
					var testContainers = ((IEnumerable<object>)testConfiguration.GetType().GetProperty("Containers").GetValue(testConfiguration)).ToArray();
					var projects = new List<CoverageProject>();

					foreach (var container in testContainers)
					{
						var project = new CoverageProject();

						var containerType = container.GetType();
						var containerData = containerType.GetProperty("ProjectData").GetValue(container);
						var containerDataType = containerData.GetType();

						project.ProjectGuid = containerType.GetProperty("ProjectGuid").GetValue(container).ToString();
						project.ProjectName = containerType.GetProperty("ProjectName").GetValue(container).ToString();
						project.TestDllFile = containerType.GetProperty("Source").GetValue(container).ToString();
						project.CoverletCoberturaFile = coverletCoberturaCollector.GetCollected(project.TestDllFile);

						project.Is64Bit = containerType.GetProperty("TargetPlatform").GetValue(container).ToString().ToLower().Equals("x64");
						project.ProjectFile = containerDataType.GetProperty("ProjectFilePath", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic).GetValue(containerData).ToString();
						project.RunSettingsFile = ThreadHelper.JoinableTaskFactory.Run(() => runSettingsRetriever.GetRunSettingsFileAsync(userRunSettings, container));
                        
						try
						{
							var defaultOutputFolder = Path.GetDirectoryName(containerDataType.GetProperty("DefaultOutputPath", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic).GetValue(containerData).ToString());
							var legacyWorkFolder = Path.Combine(Path.GetDirectoryName(defaultOutputFolder), "fine-code-coverage");
							Directory.Delete(legacyWorkFolder, true);
						}
						catch
						{
							// ignore
						}
						
						projects.Add(project);
					}

					DisposeCoberturaCollector();

					_reloadCoverageThread = new Thread(() =>
					{
						try
						{
							// compute coverage

							FCCEngine.ReloadCoverage(projects.ToArray(), darkMode);

							// update margins

							{
								UpdateMarginTagsEventArgs updateMarginTagsEventArgs = null;

								try
								{
									updateMarginTagsEventArgs = new UpdateMarginTagsEventArgs
									{
									};
								}
								catch
								{
									// ignore
								}
								finally
								{
									UpdateMarginTags?.Invoke(this, updateMarginTagsEventArgs);
								}
							}

							// update output window

							{
								UpdateOutputWindowEventArgs updateOutputWindowEventArgs = null;

								try
								{
									updateOutputWindowEventArgs = new UpdateOutputWindowEventArgs
									{
										HtmlContent = File.ReadAllText(FCCEngine.HtmlFilePath)
									};
								}
								catch
								{
									// ignore
								}
								finally
								{
									UpdateOutputWindow?.Invoke(this, updateOutputWindowEventArgs);
								}
							}

							// log

							Logger.Log("================================== DONE ===================================");
						}
						catch (Exception exception)
						{
							if (!(exception is ThreadAbortException) && _reloadCoverageThread != null)
							{
								Logger.Log("Error", exception);
								Logger.Log("================================== ERROR ==================================");
							}
						}
					});

					_reloadCoverageThread.Start();
				}

				if (e.State == TestOperationStates.Canceled)
                {
					DisposeCoberturaCollector();
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
	public interface SVsColorThemeService
	{
	}

	public class UpdateMarginTagsEventArgs : EventArgs
	{
	}

	public class UpdateOutputWindowEventArgs : EventArgs
	{
		public string HtmlContent { get; set; }
	}
}
