using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IProjectFilePropertyWriter))]
    public class ProjectFilePropertyWriter : IProjectFilePropertyWriter
    {
        public async System.Threading.Tasks.Task<bool> WritePropertyAsync(IVsHierarchy projectHierarchy, string propertyName, string value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (projectHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
            {
                var result = vsBuildPropertyStorage.GetPropertyValue(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, out var v);
                if (result == VSConstants.S_OK && v == value)
                {
                    return true;
                }
                
                return vsBuildPropertyStorage.SetPropertyValue(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, value) == VSConstants.S_OK;
                
            }

            return false;
        }

        public async System.Threading.Tasks.Task<bool> RemovePropertyAsync(IVsHierarchy pHierProj, string propertyName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (pHierProj is IVsBuildPropertyStorage vsBuildPropertyStorage)
            {
                return vsBuildPropertyStorage.RemoveProperty(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE) == VSConstants.S_OK;
            }
            return false;
        }
    }

}
