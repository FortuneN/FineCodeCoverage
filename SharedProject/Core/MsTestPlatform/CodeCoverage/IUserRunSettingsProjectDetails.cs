using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IUserRunSettingsProjectDetails
    {
        List<string> ExcludedReferencedProjects { get; set; }
        List<string> IncludedReferencedProjects { get; set; }
        string OutputFolder { get; set; }
        IMsCodeCoverageOptions Settings { get; set; }
        string TestDllFile { get; set; }
    }
}
