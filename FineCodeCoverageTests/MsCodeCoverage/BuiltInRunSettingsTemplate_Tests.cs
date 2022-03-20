using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    public class BuiltInRunSettingsTemplate_ConfigureCustom_Tests
    {
        [Test]
        public void Should_Add_Replaceable_RunConfiguration_If_Not_Present()
        {
            var custom = $@"
            <RunSettings>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <Format>Cobertura</Format>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new TestRunSettingsTemplateReplacements
            {
                ResultsDirectory = "results directory",
                TestAdapter = "ms collector path"
            };
            

            var expected = $@"
            <RunSettings>
                <RunConfiguration>
                    <ResultsDirectory>{replacements.ResultsDirectory}</ResultsDirectory>
                    <TestAdaptersPaths>{replacements.TestAdapter}</TestAdaptersPaths>
                    <CollectSourceInformation>False</CollectSourceInformation>
                </RunConfiguration>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            ConfiguredCustomReplaceTest(custom, expected, replacements);

        }

        [Test]
        public void Should_Add_Replaceable_ResultsDirectory_And_TestAdaptersPath_If_Not_Present()
        {
            var custom = $@"
            <RunSettings>
                <RunConfiguration>
                </RunConfiguration>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <Format>Cobertura</Format>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new TestRunSettingsTemplateReplacements
            {
                ResultsDirectory = "results directory",
                TestAdapter = "ms collector path"
            };
            

            var expected = $@"
            <RunSettings>
                <RunConfiguration>
                    <ResultsDirectory>{replacements.ResultsDirectory}</ResultsDirectory>
                    <TestAdaptersPaths>{replacements.TestAdapter}</TestAdaptersPaths>
                </RunConfiguration>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            ConfiguredCustomReplaceTest(custom, expected, replacements);

        }
    
        [Test]
        public void Should_Add_Replaceable_DataCollectionRunSettings_If_Not_Present()
        {
            var custom = $@"
            <RunSettings>
                <RunConfiguration>
                    <ResultsDirectory>Path</ResultsDirectory>
                    <TestAdaptersPaths>Path</TestAdaptersPaths>
                </RunConfiguration>
            </RunSettings>";

            var replacements = new TestRunSettingsTemplateReplacements
            {
                AttributesExclude = "<AttributesExclude/>",
                AttributesInclude = "<AttributesInclude/>",
                CompanyNamesExclude = "<CompanyNamesExclude/>",
                CompanyNamesInclude = "<CompanyNamesInclude/>",
                FunctionsExclude = "<FunctionsExclude/>",
                FunctionsInclude = "<FunctionsInclude/>",
                ModulePathsInclude = "<ModulePathsInclude/>",
                ModulePathsExclude = "<ModulePathsExclude/>",
                PublicKeyTokensExclude = "<PublicKeyTokensExclude/>",
                PublicKeyTokensInclude = "<PublicKeyTokensInclude/>",
                SourcesExclude = "<SourcesExclude/>",
                SourcesInclude = "<SourcesInclude/>",

                Enabled = "enabledreplaced"
            };
            
            var expected = $@"
            <RunSettings>
                <RunConfiguration>
                    <ResultsDirectory>Path</ResultsDirectory>
                    <TestAdaptersPaths>Path</TestAdaptersPaths>
                </RunConfiguration>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='{replacements.Enabled}'>
                            <Configuration>
                              <CodeCoverage>
                                <ModulePaths>
                                  <Exclude>
                                    {replacements.ModulePathsExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.ModulePathsInclude}
                                  </Include>
                                </ModulePaths>
                                <Functions>
                                  <Exclude>
                                    {replacements.FunctionsExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.FunctionsInclude}
                                  </Include>
                                </Functions>
                                <Attributes>
                                  <Exclude>
                                    {replacements.AttributesExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.AttributesInclude}
                                  </Include>
                                </Attributes>
                                <Sources>
                                  <Exclude>
                                    {replacements.SourcesExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.SourcesInclude}
                                  </Include>
                                </Sources>
                                <CompanyNames>
                                  <Exclude>
                                    {replacements.CompanyNamesExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.CompanyNamesInclude}
                                  </Include>
                                </CompanyNames>
                                <PublicKeyTokens>
                                  <Exclude>
                                    {replacements.PublicKeyTokensExclude}
                                  </Exclude>
                                  <Include>
                                    {replacements.PublicKeyTokensInclude}
                                  </Include>
                                </PublicKeyTokens>
                              </CodeCoverage>
                              <Format>Cobertura</Format>
                              <FCCGenerated/>
                            </Configuration>
                      </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            ConfiguredCustomReplaceTest(custom, expected, replacements);
        }

        [Test]

        public void Should_Add_Replaceable_DataCollectors_If_Not_Present()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_Add_Replaceable_Ms_DataCollector_If_Not_Present()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_AddEnabledReplacementAttributeIfNotPresent_To_Existing_Ms_DataCollector()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_Add_Configuration_Element_If_Not_Present_To_Existing_Ms_DataCollector()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_Add_Cobertura_Format_To_Existing_Configuration_Element_If_Not_Present()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_Correct_Existing_Format_To_Cobertura_When_Different_Format()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_Add_FCCGenerated_To_Existing_Configuration_Element()
        {
            throw new NotImplementedException();
        }

        private void ConfiguredCustomReplaceTest(string custom,string expected,IRunSettingsTemplateReplacements replacements)
        {
            var builtInRunSettingsTemplate = new BuiltInRunSettingsTemplate();

            var customTemplate = builtInRunSettingsTemplate.ConfigureCustom(custom);

            var replaced = builtInRunSettingsTemplate.Replace(customTemplate, replacements).Replaced;

            XmlAssert.NoXmlDifferences(expected, replaced);
        }
    }
}
