using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

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

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                var packageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
                shell.LoadPackage(ref packageToBeLoadedGuid, out var _);

                var outputWindowInitializedFile = Path.Combine(fccEngine.AppDataFolderPath, "outputWindowInitialized");

                if (File.Exists(outputWindowInitializedFile))
                {
                    await OutputToolWindowCommand.Instance.FindToolWindowAsync();
                }
                else
                {
                    // for first time users, the window is automatically docked 
                    await OutputToolWindowCommand.Instance.ShowToolWindowAsync();
                    File.WriteAllText(outputWindowInitializedFile, string.Empty);
                }
            }

        }
    }

}

