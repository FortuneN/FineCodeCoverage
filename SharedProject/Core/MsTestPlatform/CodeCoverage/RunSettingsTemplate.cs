using FineCodeCoverage.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IRunSettingsTemplate))]
    internal class RunSettingsTemplate : IRunSettingsTemplate
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

        private readonly string template;

        public string RunConfigurationElement { get; }

        private string ResultsDirectoryElement { get; }
        public string TestAdaptersPathElement { get; }
        public string DataCollectionRunSettingsElement { get; }
        public string DataCollectorsElement { get; }
        public string MsDataCollectorElement { get; }

        private const string fccMarkerElementName = "FCCGenerated";
        private string msDataCollectorConfigurationElement;
        private string msDataCollectorCodeCoverageElement;
        
        private readonly List<(string elementName, string value)> recommendedYouDoNotChangeElementsNetCore = new List<(string elementName, string value)>
        {
            ("UseVerifiableInstrumentation", "True"),
            ("AllowLowIntegrityProcesses", "True"),
            ("CollectFromChildProcesses", "True"),
            ("CollectAspDotNet", "False")
        };

        private readonly List<(string elementName, string value)> recommendedYouDoNotChangeElementsNetFramework = new List<(string elementName, string value)>
        {
            ("UseVerifiableInstrumentation", "False"),
            ("AllowLowIntegrityProcesses", "True"),
            ("CollectFromChildProcesses", "True"),
            ("CollectAspDotNet", "False")
        };

        private class TemplateReplaceResult : ITemplateReplacementResult
        {
            public string Replaced { get; set; }

            public bool ReplacedTestAdapter { get; set;}
        }


        public RunSettingsTemplate()
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
            msDataCollectorCodeCoverageElement = $@"
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
";
            msDataCollectorConfigurationElement = $@"
        <Configuration>
          {msDataCollectorCodeCoverageElement}
          <Format>Cobertura</Format>
          <{fccMarkerElementName}/>
        </Configuration>
";
            MsDataCollectorElement = $@"
      <DataCollector friendlyName='Code Coverage' enabled='{replacementLookups.Enabled}'>
        {msDataCollectorConfigurationElement}
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

            template = $@"<?xml version='1.0' encoding='utf-8'?>
<RunSettings>
{RunConfigurationElement}
{DataCollectionRunSettingsElement}
</RunSettings>
";
        }

        public override string ToString()
        {
            return template;
        }

        public ITemplateReplacementResult ReplaceTemplate(
            string runSettingsTemplate, 
            IRunSettingsTemplateReplacements replacements, 
            bool isNetFramework
        )
        {
            var replacedTestAdapter = HasReplaceableTestAdapter(runSettingsTemplate);
            var replacedRunSettingsTemplate = Replace(runSettingsTemplate, replacements);

            return new TemplateReplaceResult
            {
                ReplacedTestAdapter = replacedTestAdapter,
                Replaced = AddRecommendedYouDoNotChangeElementsIfNotProvided(replacedRunSettingsTemplate,isNetFramework)
            };
        }

        private string AddRecommendedYouDoNotChangeElementsIfNotProvided(string replacedRunSettingsTemplate, bool isNetFramework)
        {
            XDocument templateDocument = null;
            try
            {
                templateDocument = XDocument.Parse(replacedRunSettingsTemplate);
            }catch(XmlException exc)
            {
                throw new MsTemplateReplacementException(exc, replacedRunSettingsTemplate);
            }
            var msDataCollectorCodeCoverageElement = GetMsDataCollectorCodeCoverageElement(templateDocument);
            if (msDataCollectorCodeCoverageElement != null)
            {
                var recommendedYouDoNotChangeElementsDetails = isNetFramework ? recommendedYouDoNotChangeElementsNetFramework : recommendedYouDoNotChangeElementsNetCore;
                foreach (var recommendedYouDoNotChangeElementDetails in recommendedYouDoNotChangeElementsDetails)
                {
                    var elementName = recommendedYouDoNotChangeElementDetails.elementName;
                    var value = recommendedYouDoNotChangeElementDetails.value;
                    var recommendedYouDoNotChangeElement = msDataCollectorCodeCoverageElement.Element(elementName);
                    if (recommendedYouDoNotChangeElement == null)
                    {
                        msDataCollectorCodeCoverageElement.Add(XElement.Parse($"<{elementName}>{value}</{elementName}>"));
                    }
                }
            }
            return templateDocument.ToXmlString();
        }

        private XElement GetMsDataCollectorCodeCoverageElement(XDocument templateDocument)
        {
            var dataCollectors = templateDocument.GetStrictDescendant("RunSettings/DataCollectionRunSettings/DataCollectors");
            var msDataCollector = RunSettingsHelper.FindMsDataCollector(dataCollectors);
            return msDataCollector.GetStrictDescendant("Configuration/CodeCoverage");
        }

        // hacky.  due to tests
        private string SafeFilePathEscape(string path)
        {
            if (path == null)
            {
                return null;
            }
            return XmlFileEscaper.Escape(path);
        }

        public string Replace(string templatedXml, IRunSettingsTemplateReplacements replacements)
        {
            return templatedXml
                .Replace(replacementLookups.ResultsDirectory, SafeFilePathEscape(replacements.ResultsDirectory))
                .Replace(replacementLookups.TestAdapter, SafeFilePathEscape(replacements.TestAdapter))
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


        #region custom
        private void EnsureRunConfigurationEssentials(XElement runConfiguration)
        {
            AddIfNotPresent(runConfiguration, "ResultsDirectory", ResultsDirectoryElement,null,true);
            AddIfNotPresent(runConfiguration, "TestAdaptersPaths", TestAdaptersPathElement, null, false);
        }

        private void EnsureRunConfiguration(XElement runSettingsElement)
        {
            AddIfNotPresent(runSettingsElement, "RunConfiguration", RunConfigurationElement, EnsureRunConfigurationEssentials,true);
        }

        private void AddIfNotPresent(XElement parent,string elementName,string elementAsString,Action<XElement> presentPath = null,bool addFirst = true)
        {
            AddIfNotPresent(parent, p => p.Element(elementName), elementAsString, presentPath, addFirst);
        }

        private void AddIfNotPresent(XElement parent, Func<XElement,XElement> find,string elementAsString, Action<XElement> presentPath = null,bool addFirst = true)
        {
            var child = find(parent);
            if (child == null)
            {
                if (addFirst)
                {
                    parent.AddFirst(XElement.Parse(elementAsString));
                }
                else
                {
                    parent.Add(XElement.Parse(elementAsString));
                }
            }
            else
            {
                presentPath?.Invoke(child);
            }
        }

        private void EnsureMsDataCollectorElement(XElement dataCollectors)
        {
            AddIfNotPresent(dataCollectors, _ => RunSettingsHelper.FindMsDataCollector(dataCollectors), MsDataCollectorElement, msDataCollector =>
            {
                AddEnabledReplacementAttributeIfNotPresent(msDataCollector);
                var msDataCollectorConfiguration = GetOrAddConfigurationElement(msDataCollector);
                AddOrCorrectFormat(msDataCollectorConfiguration);
                AddFCCGeneratedIfNotPresent(msDataCollectorConfiguration);

            });
        }

        private XElement GetOrAddConfigurationElement(XElement msDataCollector)
        {
            AddIfNotPresent(msDataCollector, "Configuration", msDataCollectorConfigurationElement, AddCodeCoverageIfNotPresent,false);
            return msDataCollector.Element("Configuration");
        }

        private void AddCodeCoverageIfNotPresent(XElement configurationElement)
        {
            AddIfNotPresent(configurationElement, "CodeCoverage", msDataCollectorCodeCoverageElement, null, true);
        }

        private void AddEnabledReplacementAttributeIfNotPresent(XElement msDataCollector)
        {
            var enabledAttribute = msDataCollector.Attribute("enabled");
            if(enabledAttribute == null)
            {
                msDataCollector.Add(new XAttribute("enabled", replacementLookups.Enabled));
            }
        }

        private void AddFCCGeneratedIfNotPresent(XElement msDataCollectorConfiguration)
        {
            if (msDataCollectorConfiguration.Element(fccMarkerElementName) == null)
            {
                msDataCollectorConfiguration.Add(XElement.Parse($"<{fccMarkerElementName}/>"));
            }
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
            AddIfNotPresent(runSettingsElement, "DataCollectionRunSettings", DataCollectionRunSettingsElement, EnsureDataCollectorsElement, false);
        }

        public string ConfigureCustom(string runSettingsTemplate)
        {
            var runSettingsDocument = XDocument.Parse(runSettingsTemplate);
            var runSettingsElement = runSettingsDocument.Element("RunSettings");

            EnsureRunConfiguration(runSettingsElement);
            EnsureMsDataCollector(runSettingsElement);

            return runSettingsDocument.ToXmlString();
        }
        #endregion

        public bool FCCGenerated(IXPathNavigable inputRunSettingDocument)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            return navigator.SelectSingleNode($"//{fccMarkerElementName}") != null;
            
        }

        public bool HasReplaceableTestAdapter(string replaceable)
        {
            return replaceable.Contains(replacementLookups.TestAdapter);
        }
    }

}
