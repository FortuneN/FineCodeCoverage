using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ICPPReferencedProjectsHelper
    {
        Task<List<IExcludableReferencedProject>> GetInstrumentableReferencedProjectsAsync(VCProject cppProject);
    }

}
