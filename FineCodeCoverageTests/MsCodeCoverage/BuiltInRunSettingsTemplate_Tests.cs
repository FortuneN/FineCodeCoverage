using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    public class BuiltInRunSettingsTemplate_Tests
    {
        [Test]
        public void Should_Be_Replaceable()
        {
            var builtInRunSettingsTemplate = new BuiltInRunSettingsTemplate();
            var template = builtInRunSettingsTemplate.Template;

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

                Enabled = "enabledreplaced",
                ResultsDirectory = "resultsdirectory",
                TestAdapter = "testadapter"
            };

            var xmlDeclaration = "<?xml version='1.0' encoding='utf-8'?>";
            var expected = xmlDeclaration +  $@"
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
                                </CodeCoverage>
                                <Format>Cobertura</Format>
                                <FCCGenerated/>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
            </RunSettings>";

            var result = builtInRunSettingsTemplate.Replace(template, replacements);

            XmlAssert.NoXmlDifferences(result.Replaced, expected);
        }
    
        [Test]
        public void Should_Be_ReplacedTestAdapter_When_Template_Has_The_FCC_TestAdapter_Placeholder()
        {
            var builtInRunSettingsTemplate = new BuiltInRunSettingsTemplate();
            var template = builtInRunSettingsTemplate.Template;
            Assert.True(builtInRunSettingsTemplate.Replace(template, new TestRunSettingsTemplateReplacements()).ReplacedTestAdapter);
        }

        [Test]
        public void Should_Be_ReplacedTestAdapter_When_Custom_Template_Does_Not_Have_FCC_TestAdapter_Placeholder()
        {
            var builtInRunSettingsTemplate = new BuiltInRunSettingsTemplate();
            var customTemplate = @"
                <RunSettings>
                    <RunConfiguration>
                        <TestAdaptersPaths>No placeholder</TestAdaptersPaths>
                </RunConfiguration>
                </RunSettings>
";
            Assert.False(builtInRunSettingsTemplate.Replace(customTemplate, new TestRunSettingsTemplateReplacements()).ReplacedTestAdapter);
        }
    }

    public class BuiltInRunSettingsTemplate_ConfigureCustom_Tests
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

            var replacements = new TestRunSettingsTemplateReplacements
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

            var replacements = new TestRunSettingsTemplateReplacements
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

            var replacements = new TestRunSettingsTemplateReplacements
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

            var replacements = new TestRunSettingsTemplateReplacements
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

            var replacements = new TestRunSettingsTemplateReplacements
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
            var builtInRunSettingsTemplate = new BuiltInRunSettingsTemplate();

            var customTemplate = builtInRunSettingsTemplate.ConfigureCustom(custom);

            var replaced = builtInRunSettingsTemplate.Replace(customTemplate, replacements).Replaced;

            XmlAssert.NoXmlDifferences(replaced, expected);
        }
    }
}
