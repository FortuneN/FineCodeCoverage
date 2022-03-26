using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System.Xml.Linq;
using System.Xml.XPath;
using FineCodeCoverageTests.Test_helpers;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    public class RunSettingsTemplate_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Replaceable_With_Recommended_You_Do_Not_Change_Elements_When_Not_Provided(bool isDotNetFramework)
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            var template = runSettingsTemplate.ToString();
            var useVerifiableInstrumentation = isDotNetFramework ? "False" : "True";

            var replacements = new RunSettingsTemplateReplacements
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

                Enabled = "enabledreplaced",
                ResultsDirectory = "resultsdirectory",
                TestAdapter = "testadapter"
            };

            var expected = XmlHelper.XmlDeclaration +  $@"
            <RunSettings>
                <RunConfiguration>
                    <ResultsDirectory>{replacements.ResultsDirectory}</ResultsDirectory>
                    <TestAdaptersPaths>{replacements.TestAdapter}</TestAdaptersPaths>
                    <CollectSourceInformation>False</CollectSourceInformation>
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
                                <UseVerifiableInstrumentation>{useVerifiableInstrumentation}</UseVerifiableInstrumentation>
                                <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>
                                <CollectFromChildProcesses>True</CollectFromChildProcesses>
                                <CollectAspDotNet>False</CollectAspDotNet>
                                </CodeCoverage>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var result = runSettingsTemplate.ReplaceTemplate(template, replacements, isDotNetFramework);

            XmlAssert.NoXmlDifferences(result.Replaced, expected);
        }
    
        [Test]
        public void Should_Be_ReplacedTestAdapter_When_Template_Has_The_FCC_TestAdapter_Placeholder()
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            var template = runSettingsTemplate.ToString();
            Assert.True(runSettingsTemplate.ReplaceTemplate(template, new RunSettingsTemplateReplacements(), true).ReplacedTestAdapter);
        }

        [Test]
        public void Should_Be_ReplacedTestAdapter_False_When_Custom_Template_Does_Not_Have_FCC_TestAdapter_Placeholder()
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            var customTemplate = @"
                <RunSettings>
                    <RunConfiguration>
                        <TestAdaptersPaths>No placeholder</TestAdaptersPaths>
                </RunConfiguration>
                </RunSettings>
            ";
            var configuredCustomTemplate = runSettingsTemplate.ConfigureCustom(customTemplate);
            Assert.False(runSettingsTemplate.ReplaceTemplate(configuredCustomTemplate, new RunSettingsTemplateReplacements(), true).ReplacedTestAdapter);
        }
    
        [TestCase("%fcc_testadapter%", true)]
        [TestCase("", false)]
        public void Should_HasReplaceableTestAdapter_When_Has_FCC_TestAdapter_Placeholder(string toReplace, bool expected)
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            Assert.AreEqual(expected,runSettingsTemplate.HasReplaceableTestAdapter(toReplace));
        }

        [Test]
        public void Should_Be_FCC_Generated_If_FCCGenerated_Element_Exists()
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            var xpathNavigable = XDocument.Parse("<FCCGenerated/>").ToXPathNavigable();
            Assert.True(runSettingsTemplate.FCCGenerated(xpathNavigable));
        }

        [Test]
        public void Should_Not_Be_FCC_Generated_If_FCCGenerated_Element_Exists()
        {
            var runSettingsTemplate = new RunSettingsTemplate();
            var xpathNavigable = XDocument.Parse("<Not/>").ToXPathNavigable();
            Assert.False(runSettingsTemplate.FCCGenerated(xpathNavigable));
        }
    
        [Test]
        public void Should_Not_Add_Recommended_You_Do_Not_Change_Elements_When_Provided()
        {
            var template = @"
            <RunSettings>
                <RunConfiguration>
                </RunConfiguration>
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='{replacements.Enabled}'>
                            <Configuration>
                                <CodeCoverage>
                                <UseVerifiableInstrumentation>False</UseVerifiableInstrumentation>
                                <AllowLowIntegrityProcesses>False</AllowLowIntegrityProcesses>
                                <CollectFromChildProcesses>False</CollectFromChildProcesses>
                                <CollectAspDotNet>True</CollectAspDotNet>
                                </CodeCoverage>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var runSettingsTemplate = new RunSettingsTemplate();
            var result = runSettingsTemplate.ReplaceTemplate(template, new RunSettingsTemplateReplacements(), true);

            XmlAssert.NoXmlDifferences(result.Replaced, template);

        }
    }

    public class RunSettingsTemplate_ConfigureCustom_Tests
    {
        private const string nonReplacedRunConfiguration = @"<RunConfiguration>
                    <ResultsDirectory>Path</ResultsDirectory>
                    <TestAdaptersPaths>Path</TestAdaptersPaths>
                </RunConfiguration>";


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

            var replacements = new RunSettingsTemplateReplacements
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

            var replacements = new RunSettingsTemplateReplacements
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
            AddedReplaceableMsDataCollectorTest("");
        }

        [Test]
        public void Should_Add_Replaceable_DataCollectors_If_Not_Present()
        {
            AddedReplaceableMsDataCollectorTest("<DataCollectionRunSettings></DataCollectionRunSettings>");
        }

        [Test]
        public void Should_Add_Replaceable_DataCollectors_If_Not_Present_No_End_tag()
        {
            AddedReplaceableMsDataCollectorTest("<DataCollectionRunSettings/>");
        }


        [Test]
        public void Should_Add_Replaceable_Ms_DataCollector_If_Not_Present()
        {
            AddedReplaceableMsDataCollectorTest("<DataCollectionRunSettings><DataCollectors></DataCollectors></DataCollectionRunSettings>");
        }

        private void AddedReplaceableMsDataCollectorTest(string customDataCollectionPart)
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                {customDataCollectionPart}
            </RunSettings>";

            var replacements = new RunSettingsTemplateReplacements
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
                {nonReplacedRunConfiguration}
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
        public void Should_AddEnabledReplacementAttributeIfNotPresent_To_Existing_Ms_DataCollector()
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage'>
                            <Configuration>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new RunSettingsTemplateReplacements
            {
                Enabled = "enabledreplaced"
            };

            var expected = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='{replacements.Enabled}'>
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
        public void Should_Add_Cobertura_Format_To_Existing_Configuration_Element_If_Not_Present()
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new RunSettingsTemplateReplacements
            {
            };

            var expected = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
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
        public void Should_Correct_Existing_Format_To_Cobertura_When_Different_Format()
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <Format>Xml</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new RunSettingsTemplateReplacements
            {
            };

            var expected = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
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
        public void Should_Add_FCCGenerated_To_Existing_Configuration_Element()
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
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

            var replacements = new RunSettingsTemplateReplacements
            {
            };

            var expected = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
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
        public void Should_Add_Configuration_Element_If_Not_Present_To_Existing_Ms_DataCollector()
        {
            var custom = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
                <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'/>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var replacements = new RunSettingsTemplateReplacements
            {
            };

            var expected = $@"
            <RunSettings>
                {nonReplacedRunConfiguration}
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

        private void ConfiguredCustomReplaceTest(string custom,string expected,IRunSettingsTemplateReplacements replacements)
        {
            var runSettingsTemplate = new RunSettingsTemplate();

            var customTemplate = runSettingsTemplate.ConfigureCustom(custom);

            var replaced = runSettingsTemplate.Replace(customTemplate, replacements);

            XmlAssert.NoXmlDifferences(replaced, expected);
        }
    }
}
