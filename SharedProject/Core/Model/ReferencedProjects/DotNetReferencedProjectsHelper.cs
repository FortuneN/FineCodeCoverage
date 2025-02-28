using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using VSLangProj;
using VSLangProj80;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(IDotNetReferencedProjectsHelper))]
    internal class DotNetReferencedProjectsHelper : IDotNetReferencedProjectsHelper
    {
        public async Task<List<IExcludableReferencedProject>> GetReferencedProjectsAsync(VSProject vsProject)
        {
            var referencedProjects = (await System.Threading.Tasks.Task.WhenAll(GetReferencedSourceProjects(vsProject).Select(GetReferencedProjectAsync))).ToList();
            return new List<IExcludableReferencedProject>(referencedProjects);
        }

        private IEnumerable<Project> GetReferencedSourceProjects(VSProject vsproject)
        {
            return vsproject.References.Cast<Reference>().Where(r => r.SourceProject != null)
                .Select(r => r.SourceProject);
        }

        private async Task<ReferencedProject> GetReferencedProjectAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var (assemblyName, isDll) = await GetAssemblyNameIsDllAsync(project);
            return new ReferencedProject(project.FullName, assemblyName, isDll);
        }

        private async Task<(string, bool)> GetAssemblyNameIsDllAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var assemblyNameProperty = project.Properties.Item(nameof(ProjectProperties3.AssemblyName));
            var assemblyName = assemblyNameProperty?.Value.ToString() ?? project.Name;
            var outputTypeProperty = project.Properties.Item(nameof(ProjectProperties3.OutputType));
            var isDll = true;
            if (outputTypeProperty != null)
            {
                prjOutputType po = (prjOutputType)Enum.Parse(typeof(prjOutputType), outputTypeProperty.Value.ToString());
                isDll = po == prjOutputType.prjOutputTypeLibrary;
            }

            return (assemblyName, isDll);
        }
    }

}
