using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Collections.Generic;
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
        IUserRunSettingsAnalysisResult Analyse(IEnumerable<ICoverageProject> coverageProjectsWithRunSettings, bool useMsCodeCoverage, string fccMsTestAdapterPath);
        IXPathNavigable AddFCCRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup, string fccMsTestAdapterPath);
    }

}
