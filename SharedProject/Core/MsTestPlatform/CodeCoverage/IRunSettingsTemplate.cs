using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface ITemplateReplaceResult
    {
        string Replaced { get; }
        
        bool ReplacedTestAdapter { get; }
    }

    internal interface IRunSettingsTemplate
    {
        string FCCMarkerElementName { get; }
        
        ITemplateReplaceResult Replace(string runSettingsTemplate, IRunSettingsTemplateReplacements replacements);
        
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
