using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IPackageInitializer))]
    internal class PackageInitializer : IPackageInitializer
    {
        private readonly IFCCEngine fccEngine;
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public PackageInitializer(
            IFCCEngine fccEngine, 
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
        {
            this.fccEngine = fccEngine;
            this.serviceProvider = serviceProvider;
        }

        [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
        public void Initialize()
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

