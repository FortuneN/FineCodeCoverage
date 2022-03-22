using NUnit.Framework;
using System.Xml.XPath;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using AutoMoq;
using Moq;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Collections.Generic;
using FineCodeCoverageTests.Test_helpers;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class UserRunSettingsService_AddFCCSettings_Tests
    {
        private AutoMoqer autoMocker;
        private UserRunSettingsService userRunSettingsService;
        private const string unchangedRunConfiguration = @"
<RunConfiguration>
    <TestAdaptersPaths>SomePath</TestAdaptersPaths>
</RunConfiguration>
";
        private const string unchangedDataCollectionRunSettings = @"
<DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName='Code Coverage'>
                <Configuration>
                    <Format>Cobertura</Format>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
";
        private readonly string noRunConfigurationSettings = 
$@"<RunSettings>
    {unchangedDataCollectionRunSettings}
</RunSettings>
";
        private const string msDataCollectorIncludeCompanyNamesReplacements = @"
<DataCollector friendlyName='Code Coverage' enabled='true'>
    <Configuration>
        <CodeCoverage>
            <ModulePaths>
                <Exclude></Exclude>
                <Include></Include>
            </ModulePaths>
            <Functions>
                <Exclude></Exclude>
                <Include></Include>
            </Functions>
            <Attributes>
                <Exclude></Exclude>
                <Include></Include>
            </Attributes>
            <Sources>
                <Exclude></Exclude>
                <Include></Include>
            </Sources>
            <CompanyNames>
                <Exclude></Exclude>
                <Include>
                    <CompanyName>Replacement</CompanyName>
                </Include>
            </CompanyNames>
            <PublicKeyTokens>
                <Exclude></Exclude>
                <Include></Include>
            </PublicKeyTokens>
        </CodeCoverage>
        <Format>Cobertura</Format>
        <FCCGenerated/>
    </Configuration>
</DataCollector>
";

        [SetUp]
        public void CreateSut()
        {
            autoMocker = new AutoMoqer();
            autoMocker.SetInstance<IRunSettingsTemplate>(new RunSettingsTemplate());
            userRunSettingsService = autoMocker.Create<UserRunSettingsService>();
        }

        [Test]
        public void Should_Not_Process_When_Runsettings_Created_From_Template()
        {
            var xPathNavigable = new Mock<IXPathNavigable>().Object;

            autoMocker = new AutoMoqer();
            autoMocker.GetMock<IRunSettingsTemplate>().Setup(runSettingsTemplate => runSettingsTemplate.FCCGenerated(xPathNavigable)).Returns(true);
            
            userRunSettingsService = autoMocker.Create<UserRunSettingsService>();

            Assert.IsNull(userRunSettingsService.AddFCCRunSettings(xPathNavigable, null, null, null));
        }

        [Test]
        public void Should_Create_Replacements()
        {
            var xPathNavigable = IXPathNavigableExtensions.CreateXPathNavigable("<RunSettings/>");

            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            var testContainers = new List<ITestContainer> { new Mock<ITestContainer>().Object};
            mockRunSettingsConfigurationInfo.SetupGet(
                    runSettingsConfigurationInfo => runSettingsConfigurationInfo.TestContainers
                ).Returns(testContainers);

            Dictionary<string, IUserRunSettingsProjectDetails> projectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>();

            var mockRunSettingsTemplateReplacementsFactory = autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>();
            mockRunSettingsTemplateReplacementsFactory.Setup(
                runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(
                    testContainers,
                    projectDetailsLookup,
                    "fccMsTestAdapterPath"
                )
            ).Returns(new TestRunSettingsTemplateReplacements());

            userRunSettingsService.AddFCCRunSettings(
                xPathNavigable, 
                mockRunSettingsConfigurationInfo.Object, 
                projectDetailsLookup, 
                "fccMsTestAdapterPath"
            );

            mockRunSettingsTemplateReplacementsFactory.VerifyAll();
        }

        [Test]
        public void Should_Add_Replaced_RunConfiguration_If_Not_Present()
        {
            var runSettings = noRunConfigurationSettings;

            var resultsDirectory = "Results_Directory";
            var testAdapter = "MsTestAdapterPath";
            var expectedRunSettings = $@"
        <RunSettings>
            <RunConfiguration>
                <ResultsDirectory>{resultsDirectory}</ResultsDirectory>
                <TestAdaptersPaths>{testAdapter}</TestAdaptersPaths>
                <CollectSourceInformation>False</CollectSourceInformation>
            </RunConfiguration>
            {unchangedDataCollectionRunSettings}
        </RunSettings>
        ";
            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                ResultsDirectory = resultsDirectory,
                TestAdapter = testAdapter
            });
        }

        [Test]
        public void Should_Add_Replaced_TestAdaptersPath_If_Not_Present()
        {
            var runSettings = $@"
        <RunSettings>
            <RunConfiguration>
            </RunConfiguration>
            {unchangedDataCollectionRunSettings}
        </RunSettings>
        ";
            var expectedRunSettings = $@"
        <RunSettings>
            <RunConfiguration>
                <TestAdaptersPaths>MsTestAdapter</TestAdaptersPaths>
            </RunConfiguration>
            {unchangedDataCollectionRunSettings}
        </RunSettings>";
            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                TestAdapter = "MsTestAdapter"
            });

        }

        [Test]
        public void Should_Replace_TestAdaptersPath_If_Present()
        {
            var runSettings = $@"
        <RunSettings>
            <RunConfiguration>
                <TestAdaptersPaths>First;%fcc_testadapter%</TestAdaptersPaths>
            </RunConfiguration>
            {unchangedDataCollectionRunSettings}
        </RunSettings>
        ";
            var expectedRunSettings = $@"
        <RunSettings>
            <RunConfiguration>
                <TestAdaptersPaths>First;MsTestAdapter</TestAdaptersPaths>
            </RunConfiguration>
            {unchangedDataCollectionRunSettings}
        </RunSettings>";
            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                TestAdapter = "MsTestAdapter"
            });
        }

        [Test]
        public void Should_Add_Replaceable_DataCollectionRunSettings_If_Not_Present()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    {msDataCollectorIncludeCompanyNamesReplacements}
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";

            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                CompanyNamesInclude = "<CompanyName>Replacement</CompanyName>",
                Enabled = "true"
            });
        }

        [Test]
        public void Should_Add_Replaceable_DataCollectors_If_Not_Present()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>

            </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    {msDataCollectorIncludeCompanyNamesReplacements}
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";

            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                CompanyNamesInclude = "<CompanyName>Replacement</CompanyName>",
                Enabled = "true"
            });
        }

        [Test]
        public void Should_Add_Replaceable_Ms_Data_Collector_If_Not_Present()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Other'/>
                    </DataCollectors>
                </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector friendlyName='Other'/>
                    {msDataCollectorIncludeCompanyNamesReplacements}
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";

            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                CompanyNamesInclude = "<CompanyName>Replacement</CompanyName>",
                Enabled = "true"
            });
        }

        [Test]
        public void Should_Replace_All_Replacements()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Other'/>
                    </DataCollectors>
                </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector friendlyName='Other'/>
                    <DataCollector friendlyName='Code Coverage' enabled='true'>
                        <Configuration>
                            <CodeCoverage>
                                <ModulePaths>
                                    <Exclude>
                                        <M>ExcludeReplacement</M>
                                    </Exclude>
                                    <Include>
                                        <M>IncludeReplacement</M>
                                    </Include>
                                </ModulePaths>
                                <Functions>
                                    <Exclude>
                                        <F>ExcludeReplacement</F>
                                    </Exclude>
                                    <Include>
                                        <F>IncludeReplacement</F>
                                    </Include>
                                </Functions>
                                <Attributes>
                                    <Exclude>
                                        <A>ExcludeReplacement</A>
                                    </Exclude>
                                    <Include>
                                        <A>IncludeReplacement</A>
                                    </Include>
                                </Attributes>
                                <Sources>
                                    <Exclude>
                                        <S>ExcludeReplacement</S>
                                    </Exclude>
                                    <Include>
                                        <S>IncludeReplacement</S>
                                    </Include>
                                </Sources>
                                <CompanyNames>
                                    <Exclude>
                                        <C>ExcludeReplacement</C>
                                    </Exclude>
                                    <Include>
                                        <C>IncludeReplacement</C>
                                    </Include>
                                </CompanyNames>
                                <PublicKeyTokens>
                                    <Exclude>
                                        <P>ExcludeReplacement</P>
                                    </Exclude>
                                    <Include>
                                        <P>IncludeReplacement</P>
                                    </Include>
                                </PublicKeyTokens>
                            </CodeCoverage>
                            <Format>Cobertura</Format>
                            <FCCGenerated/>
                        </Configuration>
            </DataCollector>
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";

            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                AttributesExclude = "<A>ExcludeReplacement</A>",
                AttributesInclude = "<A>IncludeReplacement</A>",
                CompanyNamesExclude = "<C>ExcludeReplacement</C>",
                CompanyNamesInclude = "<C>IncludeReplacement</C>",
                FunctionsExclude = "<F>ExcludeReplacement</F>",
                FunctionsInclude = "<F>IncludeReplacement</F>",
                ModulePathsExclude = "<M>ExcludeReplacement</M>",
                ModulePathsInclude = "<M>IncludeReplacement</M>",
                PublicKeyTokensExclude = "<P>ExcludeReplacement</P>",
                PublicKeyTokensInclude = "<P>IncludeReplacement</P>",
                SourcesExclude = "<S>ExcludeReplacement</S>",
                SourcesInclude = "<S>IncludeReplacement</S>",
                Enabled = "true"
            });
        }

        [Test]
        public void Should_Add_Missing_Format_Cobertura_To_Existing_Ms_Data_Collector()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector friendlyName='Code Coverage' enabled='true'>
                            <Configuration>
                                <CodeCoverage>
                                    <CompanyNames>
                                        <Include>
                                            %fcc_companynames_include%
                                            <CompanyName>Other</CompanyName>
                                        </Include>
                                    </CompanyNames>
                                </CodeCoverage>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector friendlyName='Code Coverage' enabled='true'>
                        <Configuration>
                            <CodeCoverage>
                                <CompanyNames>
                                    <Include>
                                        <CompanyName>Replacement</CompanyName>
                                        <CompanyName>Other</CompanyName>
                                    </Include>
                                </CompanyNames>
                            </CodeCoverage>
                            <Format>Cobertura</Format>
                        </Configuration>
                    </DataCollector>
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";
            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements
            {
                CompanyNamesInclude = "<CompanyName>Replacement</CompanyName>",
                CompanyNamesExclude = "Not replaced",
                Enabled = "true"
            });
        }

        [Test]
        public void Should_Add_Missing_Configuration_Format_Cobertura_To_Existing_Ms_Data_Collector()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0' enabled='true'>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0' enabled='true'>
                        <Configuration>
                            <Format>Cobertura</Format>
                        </Configuration>
                    </DataCollector>
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";
            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements());
        }

        [Test]
        public void Should_Change_Format_To_Cobertura_For_Existing_Ms_Data_Collector()
        {
            var runSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                    <DataCollectors>
                        <DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0' enabled='true'>
                            <Configuration>
                                <Format>Xml</Format>
                            </Configuration>
                        </DataCollector>
                    </DataCollectors>
                </DataCollectionRunSettings>
        </RunSettings>
        ";

            var expectedRunSettings = $@"
        <RunSettings>
            {unchangedRunConfiguration}
            <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0' enabled='true'>
                        <Configuration>
                            <Format>Cobertura</Format>
                        </Configuration>
                    </DataCollector>
                </DataCollectors>
            </DataCollectionRunSettings>
        </RunSettings>
        ";

            TestAddFCCSettings(runSettings, expectedRunSettings, new TestRunSettingsTemplateReplacements());
        }

        [Test]
        public void Should_Add_Replaced_RunConfiguration_And_Add_Replaceable_DataCollectionRunSettings_If_Neither_Present()
        {
            var expectedRunSettings = @"
                <RunSettings>
                    <RunConfiguration>
                        <ResultsDirectory></ResultsDirectory>
                        <TestAdaptersPaths></TestAdaptersPaths>
                        <CollectSourceInformation>False</CollectSourceInformation>
                    </RunConfiguration>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector friendlyName='Code Coverage' enabled='true'>
                                    <Configuration>
                                        <CodeCoverage>
                                            <ModulePaths>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </ModulePaths>
                                            <Functions>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </Functions>
                                            <Attributes>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </Attributes>
                                            <Sources>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </Sources>
                                            <CompanyNames>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </CompanyNames>
                                            <PublicKeyTokens>
                                                <Exclude></Exclude>
                                                <Include></Include>
                                            </PublicKeyTokens>
                                        </CodeCoverage>
                                        <Format>Cobertura</Format>
                                        <FCCGenerated/>
                                    </Configuration>
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>                
";
            TestAddFCCSettings("<RunSettings/>", expectedRunSettings, new TestRunSettingsTemplateReplacements { Enabled = "true"});
        }

        private void TestAddFCCSettings(string runSettings, string expectedFccRunSettings, IRunSettingsTemplateReplacements runSettingsTemplateReplacements)
        {
            var actualRunSettings = AddFCCSettings(runSettings, runSettingsTemplateReplacements);
            
            XmlAssert.NoXmlDifferences(actualRunSettings, expectedFccRunSettings);
        }


        private string AddFCCSettings(string runSettings, IRunSettingsTemplateReplacements runSettingsTemplateReplacements)
        {
            var xpathNavigable = IXPathNavigableExtensions.CreateXPathNavigable(runSettings);
            var mockRunSettingsTemplateReplacementsFactory = autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>();
            mockRunSettingsTemplateReplacementsFactory.Setup(
                runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(
                    It.IsAny<IEnumerable<ITestContainer>>(),
                    It.IsAny<Dictionary<string, IUserRunSettingsProjectDetails>>(),
                    It.IsAny<string>()
                )
            ).Returns(runSettingsTemplateReplacements);
            return userRunSettingsService.AddFCCRunSettings(
                xpathNavigable, new Mock<IRunSettingsConfigurationInfo>().Object, null, null).DumpXmlContents();
        }


    }
}
