using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal class ReloadCoverageRequest
    {
        public bool Proceed { get; set; }
        public List<CoverageProject> CoverageProjects { get; set; }

        public static ReloadCoverageRequest Cancel()
        {
            return new ReloadCoverageRequest();
        }
        public static ReloadCoverageRequest Cover(List<CoverageProject> coverageProjects)
        {
            return new ReloadCoverageRequest { Proceed = true, CoverageProjects = coverageProjects };
        }
    }

}