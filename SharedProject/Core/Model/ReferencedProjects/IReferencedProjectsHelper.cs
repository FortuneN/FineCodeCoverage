using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IReferencedProjectsHelper
    {

        // todo - should not need ms build workspaces or parsing the project file
        Task<List<IExcludableReferencedProject>> GetReferencedProjectsAsync(
            string projectFile, Func<XElement> projectFileXElementProvider);
    }
}
