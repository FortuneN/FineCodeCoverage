using FineCodeCoverage.Output;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.Model
{
    // todo - remove this ? Should not be necessary
    [Export(typeof(IProjectFileReferencedProjectsHelper))]
    internal class ProjectFileReferencedProjectsHelper : IProjectFileReferencedProjectsHelper

    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public ProjectFileReferencedProjectsHelper(ILogger logger)
        {
            this.logger = logger;
        }

        public List<IExcludableReferencedProject> GetReferencedProjects(
            string projectFile, XElement projectFileXElement
        )
        {
            /*
			<ItemGroup>
				<ProjectReference Include="..\BranchCoverage\Branch_Coverage.csproj" />
				<ProjectReference Include="..\FxClassLibrary1\FxClassLibrary1.csproj"></ProjectReference>
			</ItemGroup>
			 */

            var xprojectReferences = projectFileXElement.XPathSelectElements($"/ItemGroup/ProjectReference[@Include]");
            var requiresDesignTimeBuild = false;
            List<string> referencedProjectFiles = new List<string>();
            foreach (var xprojectReference in xprojectReferences)
            {
                var referencedProjectProjectFile = xprojectReference.Attribute("Include").Value;
                if (referencedProjectProjectFile.Contains("$("))
                {
                    logger.Log($"Cannot exclude referenced project {referencedProjectProjectFile} of {projectFile} with {ReferencedProject.excludeFromCodeCoveragePropertyName}.  Cannot use MSBuildWorkspace");
                }
                else
                {
                    if (!Path.IsPathRooted(referencedProjectProjectFile))
                    {
                        referencedProjectProjectFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectFile), referencedProjectProjectFile));
                    }
                    referencedProjectFiles.Add(referencedProjectProjectFile);
                }

            }

            if (requiresDesignTimeBuild)
            {
                return new List<IExcludableReferencedProject>();

            }

            return referencedProjectFiles.Select(referencedProjectProjectFile => (IExcludableReferencedProject)new ReferencedProject(referencedProjectProjectFile)).ToList();
        }
    }
}
