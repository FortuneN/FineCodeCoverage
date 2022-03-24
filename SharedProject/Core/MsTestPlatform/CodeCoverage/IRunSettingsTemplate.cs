using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface ITemplateReplacementResult
    {
        string Replaced { get; }
        
        bool ReplacedTestAdapter { get; }
    }

    internal interface IRunSettingsTemplate
    {
        ITemplateReplacementResult ReplaceTemplate(string runSettingsTemplate, IRunSettingsTemplateReplacements replacements);
        string Replace(string templatedXml, IRunSettingsTemplateReplacements replacements);
        
        string ConfigureCustom(string runSettingsTemplate);

        string DataCollectionRunSettingsElement { get; }
        
        string DataCollectorsElement { get; }
        
        string MsDataCollectorElement { get; }
        
        string RunConfigurationElement { get; }
        
        string TestAdaptersPathElement { get; }

        bool FCCGenerated(IXPathNavigable inputRunSettingDocument);
        bool HasReplaceableTestAdapter(string replaceable);
    }

}
