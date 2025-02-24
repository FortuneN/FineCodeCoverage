using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FineCodeCoverage.Core.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IVsRunSettingsWriter))]
    internal class VsRunSettingsWriter : IVsRunSettingsWriter
    {
        private const string projectRunSettingsFilePathElementName = "RunSettingsFilePath";
        private readonly IServiceProvider serviceProvider;
        private readonly IProjectSaver projectSaver;
        private readonly IProjectFilePropertyWriter projectFilePropertyWriter;

        [ImportingConstructor]
        public VsRunSettingsWriter(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IProjectSaver projectSaver,
            IProjectFilePropertyWriter projectFilePropertyWriter
        )
        {
            this.serviceProvider = serviceProvider;
            this.projectSaver = projectSaver;
            this.projectFilePropertyWriter = projectFilePropertyWriter;
        }

        public async Task<bool> WriteRunSettingsFilePathAsync(Guid projectGuid, string projectRunSettingsFilePath)
        {
            var success = false;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(vsSolution);
            if (vsSolution.GetProjectOfGuid(ref projectGuid, out var vsHierarchy) == VSConstants.S_OK)
            {
                success = await projectFilePropertyWriter.WritePropertyAsync(vsHierarchy, projectRunSettingsFilePathElementName, projectRunSettingsFilePath);
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
                ok = await projectFilePropertyWriter.RemovePropertyAsync(vsHierarchy, projectRunSettingsFilePathElementName);
                if (ok)
                {
                    await this.projectSaver.SaveProjectAsync(vsHierarchy);
                }
            }
            return ok;
        }

    }

}
