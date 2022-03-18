using NUnit.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Core.Utilities;
using Moq;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverage_UserRunSettingsService_Suitability_Tests
    {
        [Test]
        public void Should_Be_Suitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage()
        {
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>",true);
            Assert.True(Suitable);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_UnSuitable_When_No_DataCollectors_Element_And_Use_MsCodeCoverage_False()
        {
            var (Suitable, _) = UserRunSettingsService.ValidateUserRunSettings("<?xml version='1.0' encoding='utf-8'?><RunSettings/>", false);
            Assert.False(Suitable);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Use_MsCodeCoverage_Dependent_When_DataCollectors_And_No_Ms_Collector(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings(
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

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Suitable_And_Specified_When_Ms_Collector_FriendlyName_And_Cobertura_Format(bool useMsCodeCoverage)
        {
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings(
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
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings(
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
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings(
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
            var (Suitable, SpecifiedMsCodeCoverage) = UserRunSettingsService.ValidateUserRunSettings(
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
            var (Suitable, _) = UserRunSettingsService.ValidateUserRunSettings("NotValid", useMsCodeCoverage);
            Assert.False(Suitable);
        }

        [Test]
        public void Should_Be_Suitable_If_All_Are_Suitable()
        {
            var mockFileUtil = new Mock<IFileUtil>();
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);
            mockFileUtil.Setup(f => f.ReadAllText("Path2")).Returns(suitableXmlAsUseMsCodeCoverage);

            var userRunSettingsService = new UserRunSettingsService(mockFileUtil.Object);

            var (Suitable, _) = userRunSettingsService.CheckUserRunSettingsSuitability(new string[] { "Path1", "Path2" }, true);
            Assert.True(Suitable);
            mockFileUtil.VerifyAll();
        }

        [Test]
        public void Should_Be_SpecifiedMsCodeCoverage_When_All_Suitable_And_Any_Specifies_Ms_Collector()
        {
            var mockFileUtil = new Mock<IFileUtil>();
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

            var userRunSettingsService = new UserRunSettingsService(mockFileUtil.Object);

            var (_, SpecifiedMsCodeCoverage) = userRunSettingsService.CheckUserRunSettingsSuitability(new string[] { "Path1", "Path2" }, true);
            Assert.True(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_SpecifiedMsCodeCoverage_False_When_All_Suitable_And_None_Specifies_Ms_Collector()
        {
            var mockFileUtil = new Mock<IFileUtil>();
            var suitableXmlAsUseMsCodeCoverage = "<?xml version='1.0' encoding='utf-8'?><RunSettings/>";
            mockFileUtil.Setup(f => f.ReadAllText("Path1")).Returns(suitableXmlAsUseMsCodeCoverage);

            var userRunSettingsService = new UserRunSettingsService(mockFileUtil.Object);

            var (_, SpecifiedMsCodeCoverage) = userRunSettingsService.CheckUserRunSettingsSuitability(new string[] { "Path1" }, true);
            Assert.False(SpecifiedMsCodeCoverage);
        }

        [Test]
        public void Should_Be_Unsuitable_If_Any_Are_Unsuitable()
        {
            var mockFileUtil = new Mock<IFileUtil>();
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

            var userRunSettingsService = new UserRunSettingsService(mockFileUtil.Object);

            var (Suitable, SpecifiedMsCodeCoverage) = userRunSettingsService.CheckUserRunSettingsSuitability(new string[] { "Path1", "Path2" }, false);
            Assert.False(Suitable);
            Assert.False(SpecifiedMsCodeCoverage);
        }
    }
}
