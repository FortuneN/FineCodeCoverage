using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ICoverageProject
    {
        string CoverageOutputFile { get; set; }
        string CoverageOutputFolder { get; set; }
        List<string> ExcludedReferencedProjects { get; }
        string FailureDescription { get; set; }
        string FailureStage { get; set; }
        bool HasFailed { get; }
        bool Is64Bit { get; set; }
        string ProjectFile { get; set; }
        XElement ProjectFileXElement { get; }
        string ProjectName { get; set; }
        string ProjectOutputFolder { get; }
        string RunSettingsFile { get; set; }
        IAppOptions Settings { get; }
        string TestDllFile { get; set; }

        bool IsDotNetSdkStyle();
        Task StepAsync(string stepName, Func<ICoverageProject, Task> action);
        Task PrepareForCoverageAsync();
    }
}