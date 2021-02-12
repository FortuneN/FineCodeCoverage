using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IInitializer))]
    internal class Initializer : IInitializer
    {
        private readonly IFCCEngine fccEngine;
        private readonly ILogger logger;
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public Initializer(
            IFCCEngine fccEngine, 
            ILogger logger, 
            ICoverageProjectFactory coverageProjectFactory,
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
        {
            this.fccEngine = fccEngine;
            this.logger = logger;
            this.coverageProjectFactory = coverageProjectFactory;
            this.serviceProvider = serviceProvider;
        }
        public void Initialize()
        {
            try
            {
                coverageProjectFactory.Initialize();
                fccEngine.Initialize();
                InitializePackageAndToolWindow();

                logger.Log($"Initialized");
            }
            catch (Exception exception)
            {
                logger.Log($"Failed Initialization", exception);
            }
        }
        [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
        private void InitializePackageAndToolWindow()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
                {
                    var packageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
                    shell.LoadPackage(ref packageToBeLoadedGuid, out var package);

                    var outputWindowInitializedFile = Path.Combine(fccEngine.AppDataFolderPath, "outputWindowInitialized");

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
    }

}

