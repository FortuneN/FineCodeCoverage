using FineCodeCoverage.Engine.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IExceptionReason
    {
        Exception Exception { get; }
        string Reason { get; }
    }
    internal interface IProjectRunSettingsFromTemplateResult
    {
        IExceptionReason ExceptionReason { get; }
        List<string> CustomTemplatePaths { get; }
        List<ICoverageProject> CoverageProjectsWithFCCMsTestAdapter { get; }
    }

    internal class TemplatedCoverageProjectRunSettingsResult : ICoverageProjectRunSettings
    {
        public ICoverageProject CoverageProject { get; set; }
        public string RunSettings { get; set; }
        public string CustomTemplatePath { get; internal set; }
        public bool ReplacedTestAdapter { get; internal set; }
    }

    internal interface ITemplatedRunSettingsService
    {
        Task<IProjectRunSettingsFromTemplateResult> GenerateAsync(IEnumerable<ICoverageProject> coverageProjectsWithoutRunSettings, string solutionDirectory, string fccMsTestAdapterPath);
        Task CleanUpAsync(List<ICoverageProject> coverageProjects);
        List<TemplatedCoverageProjectRunSettingsResult> CreateProjectsRunSettings(
            IEnumerable<ICoverageProject> coverageProjects,
            string solutionDirectory,
            string fccMsTestAdapterPath
        );

    }
}
