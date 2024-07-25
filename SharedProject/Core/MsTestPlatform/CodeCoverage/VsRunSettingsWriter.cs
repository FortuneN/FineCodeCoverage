using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FineCodeCoverage.Core.MsTestPlatform.CodeCoverage;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IVsRunSettingsWriter))]
    internal class VsRunSettingsWriter : IVsRunSettingsWriter
    {
        private const string projectRunSettingsFilePathElementName = "RunSettingsFilePath";
        private readonly IServiceProvider serviceProvider;
        private readonly IProjectSaver projectSaver;

        [ImportingConstructor]
        public VsRunSettingsWriter(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IProjectSaver projectSaver
        )
        {
            this.serviceProvider = serviceProvider;
            this.projectSaver = projectSaver;
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
                    success = vsBuildPropertyStorage.SetPropertyValue(projectRunSettingsFilePathElementName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, projectRunSettingsFilePath) == VSConstants.S_OK;
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
                    ok = vsBuildPropertyStorage.RemoveProperty(projectRunSettingsFilePathElementName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE) == VSConstants.S_OK;

                    if (ok)
                    {
                        this.projectSaver.SaveProject(vsHierarchy);
                    }
                }
            }
            return ok;
        }

    }

}
