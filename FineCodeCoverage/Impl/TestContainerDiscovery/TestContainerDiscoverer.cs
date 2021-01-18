﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
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
using System.Reflection;

namespace FineCodeCoverage.Impl
{
    [Name(Vsix.TestContainerDiscovererName)]
	[Export(typeof(TestContainerDiscoverer))]
	[Export(typeof(ITestContainerDiscoverer))]
	internal class TestContainerDiscoverer : ITestContainerDiscoverer
	{
		private Thread _reloadCoverageThread;
		public event EventHandler TestContainersUpdated;
		private readonly IServiceProvider _serviceProvider;
		public static event UpdateMarginTagsDelegate UpdateMarginTags;
		public static event UpdateOutputWindowDelegate UpdateOutputWindow;
		public Uri ExecutorUri => new Uri($"executor://{Vsix.Code}.Executor/v1");
		public IEnumerable<ITestContainer> TestContainers => Enumerable.Empty<ITestContainer>();
		public delegate void UpdateMarginTagsDelegate(object sender, UpdateMarginTagsEventArgs e);
		public delegate void UpdateOutputWindowDelegate(object sender, UpdateOutputWindowEventArgs e);
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
			_serviceProvider = serviceProvider;

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

		private void StopCoverageProcess()
		{
			try
			{
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

					var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

					CoverageProject[] projects = null;
					try
					{
						var testConfiguration = new Operation(e.Operation).Configuration;

						var userRunSettings = testConfiguration.UserRunSettings;
						var runSettingsRetriever = new RunSettingsRetriever();
						var testContainers = testConfiguration.Containers;

						projects = testConfiguration.Containers.Select(container =>
						{
							var project = new CoverageProject();
							project.ProjectName = container.ProjectName;
							project.TestDllFile = container.Source;
							project.Is64Bit = container.TargetPlatform.ToString().ToLower().Equals("x64");

							var containerData = container.ProjectData;
							project.ProjectFile = container.ProjectData.ProjectFilePath;
							project.RunSettingsFile = ThreadHelper.JoinableTaskFactory.Run(() => runSettingsRetriever.GetRunSettingsFileAsync(userRunSettings, containerData));
							return project;
						}).ToArray();

					}catch(Exception exc)
                    {
						throw new Exception("Error test container discoverer reflection",exc);
                    }
					

					_reloadCoverageThread = new Thread(() =>
					{
						try
						{
							// compute coverage

							FCCEngine.ReloadCoverage(projects, darkMode);

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
