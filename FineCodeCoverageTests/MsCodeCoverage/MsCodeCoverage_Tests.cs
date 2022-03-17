using FineCodeCoverage.Engine.MsTestPlatform;
using Moq;
using NUnit.Framework;
using System.Xml;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.IO;
using System.Globalization;
using System.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Org.XmlUnit.Builder;
using System.Linq.Expressions;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverage_User_RunSettings_Suitability_Tests
    {
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;

        [SetUp]
        public void CreateSut()
        {
            var mockBuiltInRunSettingsTemplate = new Mock<IBuiltInRunSettingsTemplate>();
            msCodeCoverageRunSettingsService = new MsCodeCoverageRunSettingsService(null, null, null, null, null, mockBuiltInRunSettingsTemplate.Object, null, null, null);
        }

        [Test]
        public void Should_Be_Suitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage()
        {
            var (Suitable, SpecifiedMsCodeCoverage) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>",true);
            Assert.True(Suitable);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_UnSuitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage_False()
        {
            var (Suitable, _) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>", false);
            Assert.False(Suitable);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Suitable_And_Specified_When_Ms_Collector_FriendlyName_And_Cobertura_Format(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                @"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector friendlyName='Code Coverage'>
                                <Configuration>
                                    <Format>Cobertura</Format>
                                </Configuration>
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", useMsCodeCoverage);
            Assert.True(Suitable);
            Assert.True(SpecifiedMsCodeCoverage);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Suitable_And_Specified_When_Ms_Collector_Uri_And_Cobertura_Format(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                @"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0'>
                                <Configuration>
                                    <Format>Cobertura</Format>
                                </Configuration>
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", useMsCodeCoverage);
            Assert.True(Suitable);
            Assert.True(SpecifiedMsCodeCoverage);
        }

        [TestCase("uri='datacollector://Microsoft/CodeCoverage/2.0'",true)]
        [TestCase("uri='datacollector://Microsoft/CodeCoverage/2.0'", false)]
        [TestCase("friendlyName='Code Coverage'", true)]
        [TestCase("friendlyName='Code Coverage'", false)]
        public void Should_Be_Use_MsCodeCoverage_Dependent_When_Ms_Collector_With_No_Format(string collectorAttribute,bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                $@"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector {collectorAttribute}>
                                <Configuration>
                                    <Format>Xml</Format>
                                </Configuration>
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", useMsCodeCoverage);
            Assert.AreEqual(useMsCodeCoverage, Suitable);
            Assert.True(SpecifiedMsCodeCoverage);
        }

        [TestCase("uri='datacollector://Microsoft/CodeCoverage/2.0'", true)]
        [TestCase("uri='datacollector://Microsoft/CodeCoverage/2.0'", false)]
        [TestCase("friendlyName='Code Coverage'", true)]
        [TestCase("friendlyName='Code Coverage'", false)]
        public void Should_Be_Use_MsCodeCoverage_Dependent_When_Ms_Collector_With_Format_And_Not_Cobertura(string collectorAttribute, bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                $@"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector {collectorAttribute}>
                                <Configuration>
                                    <Format>Xml</Format>
                                </Configuration>
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", useMsCodeCoverage);
            Assert.AreEqual(useMsCodeCoverage, Suitable);
            Assert.True(SpecifiedMsCodeCoverage);
        }

        // to decide - what to do when XPlat is provided

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Unsuitable_If_Invalid(bool useMsCodeCoverage)
        {
            var (Suitable, _) = MsCodeCoverageRunSettingsService.ValidateUserRunSettings("NotValid", useMsCodeCoverage);
            Assert.False(Suitable);
        }
    }

    internal class MsCodeCoverage_IRunSettingsServiceTest
    {
        private readonly List<Expression<Func<IMsCodeCoverageOptions, string[]>>> includeExcludeSetups = new List<Expression<Func<IMsCodeCoverageOptions, string[]>>>
            {
                s => s.AttributesExclude,
                s => s.AttributesInclude,
                s => s.CompanyNamesExclude,
                s => s.CompanyNamesInclude,
                s => s.FunctionsExclude,
                s => s.FunctionsInclude,
                s => s.ModulePathsExclude,
                s => s.ModulePathsInclude,
                s => s.PublicKeyTokensExclude,
                s => s.PublicKeyTokensInclude,
                s => s.SourcesExclude,
                s => s.SourcesInclude
            };
        private const string MsCodeCoveragePath = "MsCodeCoveragePath";
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private const string xmlDeclaration = "<?xml version='1.0' encoding='utf-8'?>";
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
                    <CompanyName>Company1</CompanyName>
                    <CompanyName>Company2</CompanyName>
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

        private string GetExpectedReplaceConfigurationRunSettings(string expectedResultsDirectory)
        {
            return $@"
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>{expectedResultsDirectory}</ResultsDirectory>
        <TestAdaptersPaths>{MsCodeCoveragePath}</TestAdaptersPaths>
        <CollectSourceInformation>False</CollectSourceInformation>
    </RunConfiguration>
    {unchangedDataCollectionRunSettings}
</RunSettings>
";
        }

        [SetUp]
        public void CreateSut()
        {
            msCodeCoverageRunSettingsService = new MsCodeCoverageRunSettingsService(
                null, null, null, null, null, new BuiltInRunSettingsTemplate(), null, null, null);
            msCodeCoverageRunSettingsService.MsCodeCoveragePath = MsCodeCoveragePath;
        }

        [Test]
        public void Should_Add_Replaced_RunConfiguration_If_Not_Present()
        {
            var runSettings = noRunConfigurationSettings;

            var resultsDirectory = "Results_Directory";

            var expectedRunSettings = GetExpectedReplaceConfigurationRunSettings(resultsDirectory);
            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceNoExcludesIncludes(resultsDirectory);
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
        }

        [TestCase("OutputX","OutputY")]
        [TestCase("OutputY", "OutputX")]
        public void Should_Replace_Results_Directory_With_The_Output_Folder_Of_The_First_TestContainer(string output1,string output2)
        {
            var runSettings = noRunConfigurationSettings;

            var expectedRunSettings = GetExpectedReplaceConfigurationRunSettings(output1);

            var mockSettings = new Mock<IMsCodeCoverageOptions>();
            mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
            var settings = mockSettings.Object;

            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1","Source2" });
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = output1,
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = settings
                    }
                },
                {
                    "Source2",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = output2,
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = settings
                    }
                }
            };
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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
        <TestAdaptersPaths>{MsCodeCoveragePath}</TestAdaptersPaths>
    </RunConfiguration>
    {unchangedDataCollectionRunSettings}
</RunSettings>";
            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceNoExcludesIncludes("");
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);

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
        <TestAdaptersPaths>First;{MsCodeCoveragePath}</TestAdaptersPaths>
    </RunConfiguration>
    {unchangedDataCollectionRunSettings}
</RunSettings>";
            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceNoExcludesIncludes("");
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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

            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceIncludeCompanyNames();
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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

            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceIncludeCompanyNames();
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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

            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceIncludeCompanyNames();
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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
                                <CompanyName>Company1</CompanyName>
                                <CompanyName>Company2</CompanyName>
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
            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceIncludeCompanyNames();
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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
            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceNoExcludesIncludes("");
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
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

            var (configurationInfo, userRunSettingsProjectDetailsLookup) = SingleSourceNoExcludesIncludes("");
            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
        }

        [Test]
        public void Should_Replace_Ms_Collector_With_Settings_From_All_Projects()
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1", "Source2" });
            var mockSettings1 = new Mock<IMsCodeCoverageOptions>();
            var mockSettings2 = new Mock<IMsCodeCoverageOptions>();

            void Setup(Mock<IMsCodeCoverageOptions> mockSettings,string suffix)
            {
                mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
                var fromProject = "FromProject" + suffix;
                foreach(var setup in includeExcludeSetups)
                {
                    mockSettings.Setup(setup).Returns(new string[] { fromProject});
                }
            }

            Setup(mockSettings1, "1");
            Setup(mockSettings2, "2");

            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings1.Object
                    }
                },
                {
                    "Source2",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings2.Object
                    }
                }
            };

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
            <DataCollector friendlyName='Code Coverage' enabled='true'>
                <Configuration>
                    <CodeCoverage>
                        <ModulePaths>
                            <Exclude>
                                <ModulePath>FromProject1</ModulePath>
                                <ModulePath>FromProject2</ModulePath>
                            </Exclude>
                            <Include>
                                <ModulePath>FromProject1</ModulePath>
                                <ModulePath>FromProject2</ModulePath>
                            </Include>
                        </ModulePaths>
                        <Functions>
                            <Exclude>
                                <Function>FromProject1</Function>
                                <Function>FromProject2</Function>
                            </Exclude>
                            <Include>
                                <Function>FromProject1</Function>
                                <Function>FromProject2</Function>
                            </Include>
                        </Functions>
                        <Attributes>
                            <Exclude>
                                <Attribute>FromProject1</Attribute>
                                <Attribute>FromProject2</Attribute>
                            </Exclude>
                            <Include>
                                <Attribute>FromProject1</Attribute>
                                <Attribute>FromProject2</Attribute>
                            </Include>
                        </Attributes>
                        <Sources>
                            <Exclude>
                                <Source>FromProject1</Source>
                                <Source>FromProject2</Source>
                            </Exclude>
                            <Include>
                                <Source>FromProject1</Source>
                                <Source>FromProject2</Source>
                            </Include>
                        </Sources>
                        <CompanyNames>
                            <Exclude>
                                <CompanyName>FromProject1</CompanyName>
                                <CompanyName>FromProject2</CompanyName>
                            </Exclude>
                            <Include>
                                <CompanyName>FromProject1</CompanyName>
                                <CompanyName>FromProject2</CompanyName>
                            </Include>
                        </CompanyNames>
                        <PublicKeyTokens>
                            <Exclude>
                                <PublicKeyToken>FromProject1</PublicKeyToken>
                                <PublicKeyToken>FromProject2</PublicKeyToken>
                            </Exclude>
                            <Include>
                                <PublicKeyToken>FromProject1</PublicKeyToken>
                                <PublicKeyToken>FromProject2</PublicKeyToken>
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

            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
        }

        [Test]
        public void Should_Have_Distinct_Replacements()
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1", "Source2" });
            var mockSettings1 = new Mock<IMsCodeCoverageOptions>();
            var mockSettings2 = new Mock<IMsCodeCoverageOptions>();

            void Setup(Mock<IMsCodeCoverageOptions> mockSettings)
            {
                mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
                foreach (var setup in includeExcludeSetups)
                {
                    mockSettings.Setup(setup).Returns(new string[] { "Distinct" });
                }
            }

            Setup(mockSettings1);
            Setup(mockSettings2);

            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings1.Object
                    }
                },
                {
                    "Source2",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings2.Object
                    }
                }
            };

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
            <DataCollector friendlyName='Code Coverage' enabled='true'>
                <Configuration>
                    <CodeCoverage>
                        <ModulePaths>
                            <Exclude>
                                <ModulePath>Distinct</ModulePath>
                            </Exclude>
                            <Include>
                                <ModulePath>Distinct</ModulePath>
                            </Include>
                        </ModulePaths>
                        <Functions>
                            <Exclude>
                                <Function>Distinct</Function>
                            </Exclude>
                            <Include>
                                <Function>Distinct</Function>
                            </Include>
                        </Functions>
                        <Attributes>
                            <Exclude>
                                <Attribute>Distinct</Attribute>
                            </Exclude>
                            <Include>
                                <Attribute>Distinct</Attribute>
                            </Include>
                        </Attributes>
                        <Sources>
                            <Exclude>
                                <Source>Distinct</Source>
                            </Exclude>
                            <Include>
                                <Source>Distinct</Source>
                            </Include>
                        </Sources>
                        <CompanyNames>
                            <Exclude>
                                <CompanyName>Distinct</CompanyName>
                            </Exclude>
                            <Include>
                                <CompanyName>Distinct</CompanyName>
                            </Include>
                        </CompanyNames>
                        <PublicKeyTokens>
                            <Exclude>
                                <PublicKeyToken>Distinct</PublicKeyToken>
                            </Exclude>
                            <Include>
                                <PublicKeyToken>Distinct</PublicKeyToken>
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
        }
        
        [Test]
        public void Should_Add_The_Test_Assembly_Regex_Escaped_To_Module_Excludes_When_IncludeTestAssembly_Is_False()
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1" });
            var mockSettings = new Mock<IMsCodeCoverageOptions>();
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings.Object,
                        TestDllFile = @"C:\SomePath\Test.dll"
                    }
                }
            };

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
            <DataCollector friendlyName='Code Coverage' enabled='true'>
                <Configuration>
                    <CodeCoverage>
                        <ModulePaths>
                            <Exclude>
                                <ModulePath>C:\\SomePath\\Test.dll</ModulePath>
                            </Exclude>
                            <Include></Include>
                        </ModulePaths>
                        <Functions>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Functions>
                        <Attributes>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Attributes>
                        <Sources>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Sources>
                        <CompanyNames>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </CompanyNames>
                        <PublicKeyTokens>
                            <Exclude>
                            </Exclude>
                            <Include>
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

            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
        }

        [Test]
        public void Should_Add_Regexed_IncludedExcluded_Referenced_Projects_To_ModulePaths()
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1" });
            var mockSettings = new Mock<IMsCodeCoverageOptions>();
            mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>{ "ExcludedReferenced"},
                        IncludedReferencedProjects = new List<string>{ "IncludedReferenced"},
                        Settings = mockSettings.Object,
                    }
                }
            };

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
            <DataCollector friendlyName='Code Coverage' enabled='true'>
                <Configuration>
                    <CodeCoverage>
                        <ModulePaths>
                            <Exclude>
                                <ModulePath>.*\\ExcludedReferenced.dll^</ModulePath>
                            </Exclude>
                            <Include>
                                <ModulePath>.*\\IncludedReferenced.dll^</ModulePath>
                            </Include>
                        </ModulePaths>
                        <Functions>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Functions>
                        <Attributes>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Attributes>
                        <Sources>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </Sources>
                        <CompanyNames>
                            <Exclude>
                            </Exclude>
                            <Include>
                            </Include>
                        </CompanyNames>
                        <PublicKeyTokens>
                            <Exclude>
                            </Exclude>
                            <Include>
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

            TestAddFCCSettings(runSettings, expectedRunSettings, configurationInfo, userRunSettingsProjectDetailsLookup);
        }
        

        private IRunSettingsConfigurationInfo GetMockedRunSettingsConfigurationInfo(IEnumerable<string> sources)
        {
            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            // need at least one
            var testContainers = sources.Select(source =>
            {
                var mockTestContainer = new Mock<ITestContainer>();
                mockTestContainer.Setup(tc => tc.Source).Returns(source);
                return mockTestContainer.Object;
            }).ToList();

            mockRunSettingsConfigurationInfo.Setup(ci => ci.TestContainers).Returns(testContainers);
            return mockRunSettingsConfigurationInfo.Object;
        }

        private (
            IRunSettingsConfigurationInfo configurationInfo,
            Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup
        ) SingleSourceIncludeCompanyNames()
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1" });
            var mockSettings = new Mock<IMsCodeCoverageOptions>();
            mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
            mockSettings.Setup(s => s.CompanyNamesInclude).Returns(new string[] { "Company1", "Company2" });
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = "",
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings.Object
                    }
                }
            };
            return (configurationInfo, userRunSettingsProjectDetailsLookup);
        }


        private (
            IRunSettingsConfigurationInfo configurationInfo,
            Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup
        ) SingleSourceNoExcludesIncludes(string resultsDirectory)
        {
            var configurationInfo = GetMockedRunSettingsConfigurationInfo(new string[] { "Source1" });
            var mockSettings = new Mock<IMsCodeCoverageOptions>();
            mockSettings.Setup(s => s.IncludeTestAssembly).Returns(true);
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails
                    {
                        OutputFolder = resultsDirectory,
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        Settings = mockSettings.Object
                    }
                }
            };
            return (configurationInfo, userRunSettingsProjectDetailsLookup);
        }

        

        private void TestAddFCCSettings(string runsettings, string expectedFccRunSettings, IRunSettingsConfigurationInfo runSettingsConfigurationInfo, Dictionary<string, MsCodeCoverageRunSettingsService.UserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup)
        {
            runsettings = xmlDeclaration + runsettings;
            msCodeCoverageRunSettingsService.userRunSettingsProjectDetailsLookup = userRunSettingsProjectDetailsLookup;
            var navigator = IXPathNavigableExtensions.GetNavigatorFromString(runsettings);

            var actualRunSettings = AddFCCSettings(navigator, runSettingsConfigurationInfo);
            XmlAssert.NoXmlDifferences(actualRunSettings, expectedFccRunSettings);
        }


        private string AddFCCSettings(XPathNavigator navigator, IRunSettingsConfigurationInfo runSettingsConfigurationInfo)
        {
            return msCodeCoverageRunSettingsService.AddFCCSettings(navigator, runSettingsConfigurationInfo).DumpXmlContents();
        }


    }

    internal static class XmlAssert
    {
        public static void NoXmlDifferences(string actual, string expected)
        {
            var diff = DiffBuilder.Compare(Input.FromString(expected)).WithTest(Input.FromString(actual)).Build();
            Assert.IsFalse(diff.HasDifferences());
        }
    }

    internal static class IXPathNavigableExtensions
    {
        public static XPathNavigator GetNavigatorFromString(string doc)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(doc);
            return xmlDocument.CreateNavigator();
        }
        public static string DumpXmlContents(this IXPathNavigable xmlPathNavigable)
        {
            var navigator = xmlPathNavigable.CreateNavigator();
            navigator.MoveToRoot();
            using (StringWriter stringWriter = new StringWriter((IFormatProvider)CultureInfo.InvariantCulture))
            {
                navigator.WriteSubtree((XmlWriter)new XmlTextWriter((TextWriter)stringWriter)
                {
                    Formatting = Formatting.Indented
                });
                return stringWriter.ToString();
            }
        }
    }
}
