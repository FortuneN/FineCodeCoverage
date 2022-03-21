using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IUserRunSettingsAnalysisResult { 
        bool Suitable { get; }
        bool SpecifiedMsCodeCoverage { get; }
        List<ICoverageProject> ProjectsWithFCCMsTestAdapter { get; }
    }

    internal interface IUserRunSettingsService
    {
        IXPathNavigable AddFCCRunSettings(IRunSettingsTemplate runSettingsTemplate, IRunSettingsTemplateReplacements replacements, IXPathNavigable inputRunSettingDocument);
        IUserRunSettingsAnalysisResult Analyse(IEnumerable<ICoverageProject> coverageProjectsWithRunSettings, bool useMsCodeCoverage,IRunSettingsTemplate runSettingsTemplate, string fccMsTestAdapterPath);
    }

}
