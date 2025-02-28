using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IUserRunSettingsProjectDetails
    {
        List<IReferencedProject> ExcludedReferencedProjects { get; set; }
        List<IReferencedProject> IncludedReferencedProjects { get; set; }
        string CoverageOutputFolder { get; set; }
        IMsCodeCoverageOptions Settings { get; set; }
        string TestDllFile { get; set; }
    }
}
