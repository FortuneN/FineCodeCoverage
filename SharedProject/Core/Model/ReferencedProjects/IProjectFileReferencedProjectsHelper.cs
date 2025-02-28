using System.Collections.Generic;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    interface IProjectFileReferencedProjectsHelper
    {
        List<IExcludableReferencedProject> GetReferencedProjects(string projectFile, XElement projectFileXElement);
    }
}
