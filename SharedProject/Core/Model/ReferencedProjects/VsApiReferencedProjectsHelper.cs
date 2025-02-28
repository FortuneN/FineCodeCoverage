using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using VSLangProj;

namespace FineCodeCoverage.Engine.Model
{

    [Export(typeof(IVsApiReferencedProjectsHelper))]
    internal class VsApiReferencedProjectsHelper : IVsApiReferencedProjectsHelper
    {
        private readonly ICPPReferencedProjectsHelper cppReferencedProjectsHelper;
        private readonly IDotNetReferencedProjectsHelper dotNetReferencedProjectsHelper;
        private AsyncLazy<DTE2> lazyDTE2;

        [ImportingConstructor]
        public VsApiReferencedProjectsHelper(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            ICPPReferencedProjectsHelper cppReferencedProjectsHelper,
            IDotNetReferencedProjectsHelper dotNetReferencedProjectsHelper
        )
        {
            lazyDTE2 = new AsyncLazy<DTE2>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (DTE2)serviceProvider.GetService(typeof(DTE));
            }, ThreadHelper.JoinableTaskFactory);
            this.cppReferencedProjectsHelper = cppReferencedProjectsHelper;
            this.dotNetReferencedProjectsHelper = dotNetReferencedProjectsHelper;
        }
        public async Task<List<IExcludableReferencedProject>> GetReferencedProjectsAsync(string projectFile)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var project = await GetProjectAsync(projectFile);

            if (project == null)
            {
                return null;
            }

            var cppProject = project.Object as VCProject;
            if (cppProject != null)
            {
                return await cppReferencedProjectsHelper.GetInstrumentableReferencedProjectsAsync(cppProject);
            }

            var vsProject = project.Object as VSProject;
            if (vsProject != null)
            {
                return await dotNetReferencedProjectsHelper.GetReferencedProjectsAsync(vsProject);
            }

            return null;
        }

        private async Task<Project> GetProjectAsync(string projectFile)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte2 = await lazyDTE2.GetValueAsync();
            // note that cannot do dte.Solution.Projects.Item(ProjectFile) - fails when dots in path
            return dte2.Solution.Projects.Cast<Project>().FirstOrDefault(p =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                //have to try here as unloaded projects will throw
                var projectFullName = "";
                try
                {
                    projectFullName = p.FullName;
                }
                catch { }
                return projectFullName == projectFile;
            });
        }
    }

}
