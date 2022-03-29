using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using Task = System.Threading.Tasks.Task;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IVsRunSettingsWriter))]
    internal class VsRunSettingsWriter : IVsRunSettingsWriter
    {
        private const string projectRunSettingsFilePathElementName = "RunSettingsFilePath";
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public VsRunSettingsWriter(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
        )
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<bool> WriteRunSettingsFilePathAsync(Guid projectGuid, string projectRunSettingsFilePath)
        {
            var success = false;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(vsSolution);
            if (vsSolution.GetProjectOfGuid(ref projectGuid, out var vsHierarchy) == VSConstants.S_OK)
            {
                if (vsHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
                {
                    // care not to use 2 !
                    success = vsBuildPropertyStorage.SetPropertyValue(projectRunSettingsFilePathElementName, null, 1, projectRunSettingsFilePath) == VSConstants.S_OK;
                }
            }
            return success;
        }

        public async Task<bool> RemoveRunSettingsFilePathAsync(Guid projectGuid)
        {
            var ok = false;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(vsSolution);
            if (vsSolution.GetProjectOfGuid(ref projectGuid, out var vsHierarchy) == VSConstants.S_OK)
            {
                if (vsHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
                {
                    ok = vsBuildPropertyStorage.RemoveProperty(projectRunSettingsFilePathElementName, null, 1) == VSConstants.S_OK;
                }
            }
            return ok;
        }

    }

}
