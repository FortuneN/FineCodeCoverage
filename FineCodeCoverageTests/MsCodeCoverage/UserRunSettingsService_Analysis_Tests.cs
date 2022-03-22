using NUnit.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Core.Utilities;
using Moq;
using AutoMoq;
using FineCodeCoverage.Engine.Model;
using System.Linq;
using System.Collections.Generic;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverage_UserRunSettingsService_Analysis_Tests
    {
        private AutoMoqer autoMocker;
        private UserRunSettingsService userRunSettingsService;
        private Mock<IFileUtil> mockFileUtil;

        [SetUp]
        public void SetUpSut()
        {
            autoMocker = new AutoMoqer();
            userRunSettingsService = autoMocker.Create<UserRunSettingsService>();
            mockFileUtil = autoMocker.GetMock<IFileUtil>();
        }

        [Test]
        public void Should_Be_Suitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage()
        {
            var (Suitable, _) = ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>",true);
            Assert.True(Suitable);
        }

        [Test]
        public void Should_Not_SpecifiedMsCodeCoverage_When_No_DataCollectors_Element()
        {
            var (_, SpecifiedMsCodeCoverage) = ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>", true);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_UnSuitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage_False()
        {
            var (Suitable, _) = ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>", false);
            Assert.False(Suitable);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Use_MsCodeCoverage_Dependent_When_DataCollectors_And_No_Ms_Collector(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                @"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", useMsCodeCoverage);
            Assert.AreEqual(useMsCodeCoverage,Suitable);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Not_SpecifiedMsCodeCoverage_When_DataCollectors_And_No_Ms_Collector()
        {
            var (_, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                @"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>
            ", true);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Suitable_And_Specified_When_Ms_Collector_FriendlyName_And_Cobertura_Format(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
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
            var (Suitable, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
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
            var (Suitable, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
                "<?xml version='1.0' encoding='utf-8'?>" +
                $@"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector {collectorAttribute}>
                                <Configuration>
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
            var (Suitable, SpecifiedMsCodeCoverage) = ValidateUserRunSettings(
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

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Unsuitable_If_Invalid(bool useMsCodeCoverage)
        {
            var (Suitable, _) = ValidateUserRunSettings("NotValid", useMsCodeCoverage);
            Assert.False(Suitable);
        }

        private (bool Suitable, bool SpecifiedMsCodeCoverage) ValidateUserRunSettings(string runSettings, bool useMsCodeCoverage)
        {
            var userRunSettingsPath = "some.runsettings";
            mockFileUtil.Setup(f => f.ReadAllText(userRunSettingsPath)).Returns(runSettings);
            return userRunSettingsService.ValidateUserRunSettings(userRunSettingsPath, useMsCodeCoverage);
        }

        [Test]
        public void Should_Be_Suitable_If_All_Are_Suitable()
        {
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);
            mockFileUtil.Setup(f => f.ReadAllText("Path2")).Returns(suitableXmlAsUseMsCodeCoverage);

            var analysisResult = userRunSettingsService.Analyse(CreateCoverageProjectsWithRunSettings(new string[] { "Path1", "Path2" }), true, null);
            Assert.True(analysisResult.Suitable);
            mockFileUtil.VerifyAll();
        }

        [Test]
        public void Should_Be_SpecifiedMsCodeCoverage_When_All_Suitable_And_Any_Specifies_Ms_Collector()
        {
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);

            var specifiesMsDataCollector = "<?xml version='1.0' encoding='utf-8'?>" +
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
                </RunSettings>";
            mockFileUtil.Setup(f => f.ReadAllText("Path2")).Returns(specifiesMsDataCollector);

            var analysisResult = userRunSettingsService.Analyse(CreateCoverageProjectsWithRunSettings(new string[] { "Path1", "Path2" }), true, null);
            Assert.True(analysisResult.SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_SpecifiedMsCodeCoverage_False_When_All_Suitable_And_None_Specifies_Ms_Collector()
        {
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);

            var analysisResult = userRunSettingsService.Analyse(CreateCoverageProjectsWithRunSettings(new string[] { "Path1" }), true, null);
            Assert.False(analysisResult.SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_Unsuitable_If_Any_Are_Unsuitable()
        {
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);
            var specifiesMsDataCollector = "<?xml version='1.0' encoding='utf-8'?>" +
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
                </RunSettings>";
            mockFileUtil.Setup(f => f.ReadAllText("Path2")).Returns(specifiesMsDataCollector);

            var analysisResult = userRunSettingsService.Analyse(CreateCoverageProjectsWithRunSettings(new string[] { "Path1", "Path2" }), false, null);
            Assert.False(analysisResult.Suitable);
            Assert.False(analysisResult.SpecifiedMsCodeCoverage);
        }


        [Test]
        public void Should_Have_Project_With_FCCMsTestAdapter_If_No_TestAdaptersPath()
        {
            var runSettingsNoTestAdaptersPath = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            var userRunSettingsPath = "some.runsettings";
            mockFileUtil.Setup(f => f.ReadAllText(userRunSettingsPath)).Returns(runSettingsNoTestAdaptersPath);
            var projectsWithTestAdapter = CreateCoverageProjectsWithRunSettings(userRunSettingsPath);
            var analysisResult = userRunSettingsService.Analyse(projectsWithTestAdapter, true, null);
            Assert.AreEqual(projectsWithTestAdapter, analysisResult.ProjectsWithFCCMsTestAdapter);
        }

        [Test]
        public void Should_Have_Project_With_FCCMsTestAdapter_If_Has_Replaceable_Test_Adapter()
        {
            var mockRunSettingsTemplate = autoMocker.GetMock<IRunSettingsTemplate>();
            mockRunSettingsTemplate.Setup(runSettingsTemplate => runSettingsTemplate.HasReplaceableTestAdapter("The paths")).Returns(true);
            var runSettingsTestAdaptersPath = @"<?xml version='1.0' encoding='utf-8'?>
                <RunSettings>
                    <RunConfiguration>
                        <TestAdaptersPaths>The paths</TestAdaptersPaths>
                    </RunConfiguration>
                </RunSettings>";

            var userRunSettingsPath = "some.runsettings";
            mockFileUtil.Setup(f => f.ReadAllText(userRunSettingsPath)).Returns(runSettingsTestAdaptersPath);
            var projectsWithTestAdapter = CreateCoverageProjectsWithRunSettings(userRunSettingsPath);
            var analysisResult = userRunSettingsService.Analyse(projectsWithTestAdapter, true, null);
            Assert.AreEqual(projectsWithTestAdapter, analysisResult.ProjectsWithFCCMsTestAdapter);
        }

        [Test]
        public void Should_Have_Project_With_FCCMsTestAdapter_If_TestAdaptersPaths_Includes_The_FCC_Path()
        {
            var fccMsTestAdapterPath = "FCCPath";
            var runSettingsTestAdaptersPath = $@"<?xml version='1.0' encoding='utf-8'?>
                <RunSettings>
                    <RunConfiguration>
                        <TestAdaptersPaths>otherPath;{fccMsTestAdapterPath}</TestAdaptersPaths>
                    </RunConfiguration>
                </RunSettings>";

            var userRunSettingsPath = "some.runsettings";
            mockFileUtil.Setup(f => f.ReadAllText(userRunSettingsPath)).Returns(runSettingsTestAdaptersPath);
            var projectsWithTestAdapter = CreateCoverageProjectsWithRunSettings(userRunSettingsPath);
            var analysisResult = userRunSettingsService.Analyse(projectsWithTestAdapter, true, fccMsTestAdapterPath);
            Assert.AreEqual(projectsWithTestAdapter, analysisResult.ProjectsWithFCCMsTestAdapter);
        }

        [Test]
        public void Should_Be_Possible_To_Have_Project_With_No_FCCMsTestAdapter()
        {
            var runSettingsTestAdaptersPath = $@"<?xml version='1.0' encoding='utf-8'?>
                <RunSettings>
                    <RunConfiguration>
                        <TestAdaptersPaths>otherPath;</TestAdaptersPaths>
                    </RunConfiguration>
                </RunSettings>";

            var userRunSettingsPath = "some.runsettings";
            mockFileUtil.Setup(f => f.ReadAllText(userRunSettingsPath)).Returns(runSettingsTestAdaptersPath);
            var projectsWithTestAdapter = CreateCoverageProjectsWithRunSettings(userRunSettingsPath);
            var analysisResult = userRunSettingsService.Analyse(projectsWithTestAdapter, true, "FCCPath");
            Assert.IsEmpty(analysisResult.ProjectsWithFCCMsTestAdapter);
        }

        private List<ICoverageProject> CreateCoverageProjectsWithRunSettings(params string[] runSettingsPaths)
        {
            return runSettingsPaths.Select(path =>
            {
                var mock = new Mock<ICoverageProject>();
                mock.Setup(cp => cp.RunSettingsFile).Returns(path);
                return mock.Object;
            }).ToList();
           
        }
    }
}
