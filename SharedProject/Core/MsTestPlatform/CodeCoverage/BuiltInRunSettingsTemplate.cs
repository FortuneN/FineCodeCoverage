using FineCodeCoverage.Core.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IBuiltInRunSettingsTemplate))]
    internal class BuiltInRunSettingsTemplate : IBuiltInRunSettingsTemplate
    {
        private class ReplacementLookups : IRunSettingsTemplateReplacements
        {
            public string Enabled { get; } = "%fcc_enabled%";
            public string ResultsDirectory { get; } = "%fcc_resultsdirectory%";
            public string TestAdapter { get; } = "%fcc_testadapter%";
            public string ModulePathsExclude { get; } = "%fcc_modulepaths_exclude%";
            public string ModulePathsInclude { get; } = "%fcc_modulepaths_include%";
            public string FunctionsExclude { get; } = "%fcc_functions_exclude%";
            public string FunctionsInclude { get; } = "%fcc_functions_include%";
            public string AttributesExclude { get; } = "%fcc_attributes_exclude%";
            public string AttributesInclude { get; } = "%fcc_attributes_include%";
            public string SourcesExclude { get; } = "%fcc_sources_exclude%";
            public string SourcesInclude { get; } = "%fcc_sources_include%";
            public string CompanyNamesExclude { get; } = "%fcc_companynames_exclude%";
            public string CompanyNamesInclude { get; } = "%fcc_companynames_include%";
            public string PublicKeyTokensExclude { get; } = "%fcc_publickeytokens_exclude%";
            public string PublicKeyTokensInclude { get; } = "%fcc_publickeytokens_include%";
        }

        private readonly ReplacementLookups replacementLookups = new ReplacementLookups();

        public string Template { get; }

        public string RunConfigurationElement { get; }

        private string ResultsDirectoryElement { get; }
        public string TestAdaptersPathElement { get; }
        public string DataCollectionRunSettingsElement { get; }
        public string DataCollectorsElement { get; }
        public string MsDataCollectorElement { get; }

        public string FCCMarkerElementName { get; } = "FCCGenerated";


        public BuiltInRunSettingsTemplate()
        {
            ResultsDirectoryElement = $"<ResultsDirectory>{replacementLookups.ResultsDirectory}</ResultsDirectory>";
            TestAdaptersPathElement = $"<TestAdaptersPaths>{replacementLookups.TestAdapter}</TestAdaptersPaths>";
            RunConfigurationElement = $@"
  <RunConfiguration>
    {ResultsDirectoryElement}
    {TestAdaptersPathElement}
    <CollectSourceInformation>False</CollectSourceInformation>
  </RunConfiguration>
";

            MsDataCollectorElement = $@"
      <DataCollector friendlyName='Code Coverage' enabled='{replacementLookups.Enabled}'>
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Exclude>
                {replacementLookups.ModulePathsExclude}
              </Exclude>
              <Include>
                {replacementLookups.ModulePathsInclude}
              </Include>
            </ModulePaths>
            <Functions>
              <Exclude>
                {replacementLookups.FunctionsExclude}
              </Exclude>
              <Include>
                {replacementLookups.FunctionsInclude}
              </Include>
            </Functions>
            <Attributes>
              <Exclude>
                {replacementLookups.AttributesExclude}
              </Exclude>
              <Include>
                {replacementLookups.AttributesInclude}
              </Include>
            </Attributes>
            <Sources>
              <Exclude>
                {replacementLookups.SourcesExclude}
              </Exclude>
              <Include>
                {replacementLookups.SourcesInclude}
              </Include>
            </Sources>
            <CompanyNames>
              <Exclude>
                {replacementLookups.CompanyNamesExclude}
              </Exclude>
              <Include>
                {replacementLookups.CompanyNamesInclude}
              </Include>
            </CompanyNames>
            <PublicKeyTokens>
              <Exclude>
                {replacementLookups.PublicKeyTokensExclude}
              </Exclude>
              <Include>
                {replacementLookups.PublicKeyTokensInclude}
              </Include>
            </PublicKeyTokens>
          </CodeCoverage>
          <Format>Cobertura</Format>
          <{FCCMarkerElementName}/>
        </Configuration>
      </DataCollector>
";
            DataCollectorsElement = $@"
    <DataCollectors>
      {MsDataCollectorElement}
    </DataCollectors>
";
            DataCollectionRunSettingsElement = $@"
  <DataCollectionRunSettings>
    {DataCollectorsElement}
  </DataCollectionRunSettings>
";

            Template = $@"<?xml version='1.0' encoding='utf-8'?>

<!-- 
Edit this xml file to configure the code coverage for FCC.See
https://docs.microsoft.com/en-us/visualstudio/test/customizing-code-coverage-analysis?view=vs-2022
for details.

The resulting runsettings file actually used for test runs is put into .fcc/fcc.runsettings
-->
<RunSettings>
{RunConfigurationElement}
{DataCollectionRunSettingsElement}
</RunSettings>
";
        }

        public string Replace(string runSettingsTemplate, IRunSettingsTemplateReplacements replacements)
        {
            return runSettingsTemplate
                      .Replace(replacementLookups.ResultsDirectory, replacements.ResultsDirectory)
                      .Replace(replacementLookups.TestAdapter, replacements.TestAdapter)

                      .Replace(replacementLookups.Enabled, replacements.Enabled)
                      .Replace(replacementLookups.ModulePathsExclude, replacements.ModulePathsExclude)
                      .Replace(replacementLookups.ModulePathsInclude, replacements.ModulePathsInclude)
                      .Replace(replacementLookups.FunctionsExclude, replacements.FunctionsExclude)
                      .Replace(replacementLookups.FunctionsInclude, replacements.FunctionsInclude)
                      .Replace(replacementLookups.AttributesExclude, replacements.AttributesExclude)
                      .Replace(replacementLookups.AttributesInclude, replacements.AttributesInclude)
                      .Replace(replacementLookups.SourcesExclude, replacements.SourcesExclude)
                      .Replace(replacementLookups.SourcesInclude, replacements.SourcesInclude)
                      .Replace(replacementLookups.CompanyNamesExclude, replacements.CompanyNamesExclude)
                      .Replace(replacementLookups.CompanyNamesInclude, replacements.CompanyNamesInclude)
                      .Replace(replacementLookups.PublicKeyTokensExclude, replacements.PublicKeyTokensExclude)
                      .Replace(replacementLookups.PublicKeyTokensInclude, replacements.PublicKeyTokensInclude);
        }

        private void EnsureRunConfigurationEssentials(XElement runConfiguration)
        {
            AddIfNotPresent(runConfiguration, "ResultsDirectory", ResultsDirectoryElement);
            AddIfNotPresent(runConfiguration, "TestAdaptersPaths", TestAdaptersPathElement);
        }

        private void EnsureRunConfiguration(XElement runSettingsElement)
        {
            AddIfNotPresent(runSettingsElement, "RunConfiguration", RunConfigurationElement, EnsureRunConfigurationEssentials);
        }

        private void AddIfNotPresent(XElement parent,string elementName,string elementAsString,Action<XElement> elsePath = null)
        {
            AddIfNotPresent(parent, p => p.Element(elementName), elementAsString, elsePath);
        }

        private void AddIfNotPresent(XElement parent, Func<XElement,XElement> find,string elementAsString, Action<XElement> elsePath = null)
        {
            var child = find(parent);
            if (child == null)
            {
                parent.Add(XElement.Parse(elementAsString));
            }
            else
            {
                elsePath?.Invoke(child);
            }
        }

        private void EnsureMsDataCollectorElement(XElement dataCollectors)
        {
            AddIfNotPresent(dataCollectors, _ => RunSettingsHelper.FindMsDataCollector(dataCollectors), MsDataCollectorElement, msDataCollector =>
            {
                AddEnabledReplacementAttribute(msDataCollector);
                var msDataCollectorConfiguration = msDataCollector.Element("Configuration");
                if(msDataCollectorConfiguration == null)
                {
                    msDataCollectorConfiguration = new XElement("Configuration");
                    msDataCollector.Add(msDataCollectorConfiguration);
                }
                AddOrCorrectFormat(msDataCollectorConfiguration);
                AddFCCGenerated(msDataCollectorConfiguration);

            });
        }

        private void AddEnabledReplacementAttribute(XElement msDataCollector)
        {
            var enabledAttribute = msDataCollector.Attribute("enabled");
            if(enabledAttribute == null)
            {
                msDataCollector.Add(new XAttribute("Enabled", replacementLookups.Enabled));
            }
        }

        private void AddFCCGenerated(XElement msDataCollector)
        {
            msDataCollector.Add(new XElement(FCCMarkerElementName));
        }

        private void AddOrCorrectFormat(XElement configuration)
        {
            var formatElement = configuration.Element("Format");
            if (formatElement == null)
            {
                configuration.Add(new XElement("Format", "Cobertura"));
            }
            else
            {
                formatElement.Value = "Cobertura";
            }
        }

        private void EnsureDataCollectorsElement(XElement dataCollectionRunSettings)
        {
            AddIfNotPresent(dataCollectionRunSettings, "DataCollectors", DataCollectorsElement, EnsureMsDataCollectorElement);
        }

        private void EnsureMsDataCollector(XElement runSettingsElement)
        {
            AddIfNotPresent(runSettingsElement, "DataCollectionRunSettings", DataCollectionRunSettingsElement, EnsureDataCollectorsElement);
        }

        public string ConfigureCustom(string runSettingsTemplate)
        {
            var runSettingsDocument = XDocument.Parse(runSettingsTemplate);
            var runSettingsElement = runSettingsDocument.Element("RunSettings");

            EnsureRunConfiguration(runSettingsElement);
            EnsureMsDataCollector(runSettingsElement);

            return runSettingsDocument.ToXmlString();
        }

        public bool FCCGenerated(IXPathNavigable inputRunSettingDocument)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            return navigator.SelectSingleNode($"//{FCCMarkerElementName}") != null;
            
        }
    }

}
