using System.Collections.Generic;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IUserRunSettingsService
    {
        IXPathNavigable AddFCCRunSettings(IBuiltInRunSettingsTemplate builtInRunSettingsTemplate, IRunSettingsTemplateReplacements replacements, IXPathNavigable inputRunSettingDocument);
        (bool Suitable, bool SpecifiedMsCodeCoverage) CheckUserRunSettingsSuitability(IEnumerable<string> userRunSettingsFiles, bool useMsCodeCoverage);
    }

}
