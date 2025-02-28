using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IVsApiReferencedProjectsHelper
    {
        Task<List<IExcludableReferencedProject>> GetReferencedProjectsAsync(string projectFile);
    }
}
