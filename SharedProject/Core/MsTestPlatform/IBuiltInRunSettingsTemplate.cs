namespace FineCodeCoverage.Engine.MsTestPlatform
{
    internal interface IBuiltInRunSettingsTemplate
    {
        string FCCMarkerElementName { get; }
        string Template { get; }
        string Replace(string runSettingsTemplate, IRunSettingsTemplateReplacements replacements);
        string ConfigureCustom(string runSettingsTemplate);

        string DataCollectionRunSettingsElement { get; }
        string DataCollectorsElement { get; }
        string MsDataCollectorElement { get; }
        string RunConfigurationElement { get; }
        string TestAdaptersPathElement { get; }

    }

}
