using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Core.Initialization
{
    internal interface IShellPackageLoader
    {
        Task LoadPackageAsync();
    }

    [Export(typeof(IShellPackageLoader))]
    internal class ShellPackageLoader : IShellPackageLoader
    {
        private IServiceProvider serviceProvider;

        [ImportingConstructor]
        public ShellPackageLoader(
            [Import(typeof(SVsServiceProvider))]
             IServiceProvider serviceProvider
        )
        {
            this.serviceProvider = serviceProvider;
        }
        public async Task LoadPackageAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                var packageToBeLoadedGuid = PackageGuids.guidOutputToolWindowPackage;
                shell.LoadPackage(ref packageToBeLoadedGuid, out var _);
            }
        }

    }

    [Export(typeof(IPackageLoader))]
    [Export(typeof(IInitializedFromTestContainerDiscoverer))]
    internal class PackageLoader : IPackageLoader, IInitializedFromTestContainerDiscoverer
    {
        private readonly IShellPackageLoader shellPackageLoader;

        public bool InitializedFromTestContainerDiscoverer { get; private set; }

        [ImportingConstructor]
        public PackageLoader(
            IShellPackageLoader shellPackageLoader
            )
        {
            this.shellPackageLoader = shellPackageLoader;
        }

        public async Task LoadPackageAsync(CancellationToken cancellationToken)
        {
            InitializedFromTestContainerDiscoverer = true;
            cancellationToken.ThrowIfCancellationRequested();
            await shellPackageLoader.LoadPackageAsync();

        }
    }
}



