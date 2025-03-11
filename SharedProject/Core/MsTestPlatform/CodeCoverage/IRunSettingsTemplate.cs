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
        ITemplateReplacementResult ReplaceTemplate(
            string runSettingsTemplate, 
            IRunSettingsTemplateReplacements replacements, 
            bool isNetFrameworkProject);
        string Replace(string templatedXml, IRunSettingsTemplateReplacements replacements);

        // returns a string representation of the runsettings xml containing markers for string replacement
        string Get();

        // returns a string representation of the runsettings xml containing markers for string replacement
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
