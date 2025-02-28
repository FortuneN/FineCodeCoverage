using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICPPReferencedProjectsHelper))]
    internal class CPPReferencedProjectsHelper : ICPPReferencedProjectsHelper
    {
        private VCProject GetReferencedVCProject(VCProjectReference projectReference)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return projectReference.ReferencedProject as VCProject
                ?? (projectReference.ReferencedProject as EnvDTE.Project)?.Object as VCProject;
        }

        private bool? IsDll(VCProject vcProject)
        {
            if (!(vcProject.Configurations is IEnumerable configurations))
                return null;

            var configuration = configurations.Cast<VCConfiguration>().FirstOrDefault();
            if (configuration == null)
                return null;

            bool isDll = configuration.ConfigurationType == ConfigurationTypes.typeDynamicLibrary;
            bool isApplication = configuration.ConfigurationType == ConfigurationTypes.typeApplication;
            if (!isDll && !isApplication)
                return null;
            return isDll;
        }

        private string GetCPPProjectReferenceProjectFilePath(VCProjectReference reference)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var vsReference = reference.Reference as VSLangProj.Reference;
            var sourceProject = vsReference.SourceProject;
            return sourceProject.FileName;
        }

        public async Task<List<IExcludableReferencedProject>> GetInstrumentableReferencedProjectsAsync(VCProject cppProject)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!(cppProject.VCReferences is IEnumerable vcReferences))
                return null;

            return vcReferences
                .OfType<VCProjectReference>()
                .Select(reference =>
                {
                    var referencedProject = GetReferencedVCProject(reference);

                    var isDll = IsDll(referencedProject);
                    return isDll.HasValue ?(IExcludableReferencedProject) new ReferencedProject(
                            GetCPPProjectReferenceProjectFilePath(reference),
                            Path.GetFileNameWithoutExtension(reference.FullPath),
                            isDll.Value
                        )
                        : null;
                })
                .Where(p => p != null)
                .ToList();
        }

    }

}
