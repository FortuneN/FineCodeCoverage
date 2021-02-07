using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Impl
{
    [Name(Vsix.TestContainerDiscovererName)]
    [Export(typeof(TestContainerDiscoverer))]
    [Export(typeof(ITestContainerDiscoverer))]
    internal class TestContainerDiscoverer : ITestContainerDiscoverer
    {
#pragma warning disable 67
        public event EventHandler TestContainersUpdated;
#pragma warning restore 67
        private readonly IServiceProvider _serviceProvider;
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
            _serviceProvider = serviceProvider;

            new Thread(() =>
            {
                try
                {
                    Logger.Initialize(_serviceProvider);

                    FCCEngine.Initialize(_serviceProvider);
                    Initialize(_serviceProvider);
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

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
            try
            {
                if (e.State == TestOperationStates.TestExecutionStarting)
                {
                    FCCEngine.StopCoverage();
                }

                if (e.State == TestOperationStates.TestExecutionFinished)
                {
                    if (!FCCEngine.CanRunCoverage())
                    {
                        return;
                    }

                    List<CoverageProject> projects = null;
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
                        }).ToList();

                    }
                    catch (Exception exc)
                    {
                        throw new Exception("Error test container discoverer reflection", exc);
                    }

                    FCCEngine.ReloadCoverage(projects);
                }
            }
            catch (Exception exception)
            {
                Logger.Log("Error processing unit test events", exception);
            }
        }
    }



}
