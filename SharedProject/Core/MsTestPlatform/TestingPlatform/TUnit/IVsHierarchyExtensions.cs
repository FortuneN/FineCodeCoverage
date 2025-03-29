using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem;
using EnvDTE;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal static class IVsHierarchyExtensions
    {
        // https://github.com/microsoft/VSProjectSystem/blob/master/doc/automation/finding_CPS_in_a_VS_project.md
        public async static Task<UnconfiguredProject> AsUnconfiguredProjectAsync(this IVsHierarchy hier)
        {
            if (hier is IVsBrowseObjectContext context)
            {
                return context.UnconfiguredProject;
            }
            else
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                object pvar;
                if (ErrorHandler.Succeeded(hier.GetProperty(4294967294U, -2027, out pvar)) && pvar is Project project)
                {
                    context = project.Object as IVsBrowseObjectContext;
                    return context.UnconfiguredProject;
                }
            }
             
            return null;
        }

        public static Project ToProject(this IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Retrieve the automation object from the root of the hierarchy.
            int hr = hierarchy.GetProperty(
                VSConstants.VSITEMID_ROOT,
                (int)__VSHPROPID.VSHPROPID_ExtObject,
                out object extObject);

            if (ErrorHandler.Succeeded(hr) && extObject is Project project)
            {
                return project;
            }

            return null;
        }

        public static Guid GetGuid(this IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            int hr = hierarchy.GetGuidProperty(
                VSConstants.VSITEMID_ROOT,
                (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                out Guid projectGuid);

            return projectGuid;
        }

        public async static Task<Guid> GetGuidAsync(this IVsHierarchy hierarchy)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return GetGuid(hierarchy);
        }
    }
}
