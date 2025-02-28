using System.Collections.Generic;
using System.Threading.Tasks;
using VSLangProj;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IDotNetReferencedProjectsHelper
    {
        Task<List<IExcludableReferencedProject>> GetReferencedProjectsAsync(VSProject vsProject);
    }
}
