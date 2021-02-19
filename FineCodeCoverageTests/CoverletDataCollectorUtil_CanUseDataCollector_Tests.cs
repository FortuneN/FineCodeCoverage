using System;
using System.Xml.Linq;
using AutoMoq;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class CoverletDataCollectorUtil_CanUseDataCollector_Tests
    {
        private AutoMoqer mocker;
        private CoverletDataCollectorUtil coverletDataCollectorUtil;

        public enum UseDataCollectorElement { True, False, Empty, None }

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverletDataCollectorUtil = mocker.Create<CoverletDataCollectorUtil>();
        }

        private void SetUpRunSettings(Mock<ICoverageProject> mockCoverageProject, Action<Mock<IRunSettingsCoverletConfiguration>> setup)
        {
            mockCoverageProject.Setup(p => p.RunSettingsFile).Returns(".runsettings");
            var mockRunSettingsCoverletConfiguration = mocker.GetMock<IRunSettingsCoverletConfiguration>();
            var mockRunSettingsCoverletConfigurationFactory = mocker.GetMock<IRunSettingsCoverletConfigurationFactory>();
            mockRunSettingsCoverletConfigurationFactory.Setup(rscf => rscf.Create()).Returns(mockRunSettingsCoverletConfiguration.Object);
            setup?.Invoke(mockRunSettingsCoverletConfiguration);
        }
        
        private void SetupDataCollectorState(Mock<ICoverageProject> mockCoverageProject, CoverletDataCollectorState coverletDataCollectorState)
        {
            SetUpRunSettings(mockCoverageProject, mrsc => mrsc.Setup(rsc => rsc.CoverletDataCollectorState).Returns(coverletDataCollectorState));
        }

        private XElement GetProjectElementWithDataCollector(UseDataCollectorElement useDataCollector)
        {
            var useDataCollectorElement = "";
            if(useDataCollector != UseDataCollectorElement.None)
            {
                var value = "";
                if(useDataCollector == UseDataCollectorElement.True)
                {
                    value = "true";
                }
                if(useDataCollector == UseDataCollectorElement.False)
                {
                    value = "false";
                }
                useDataCollectorElement = $"<PropertyGroup><UseDataCollector>{value}</UseDataCollector></PropertyGroup>";
            }
            
            return XElement.Parse($@"<Project>
{useDataCollectorElement}
</Project>");
        }

        

        [Test]
        public void Should_Factory_Create_Configuration_And_Read_CoverageProject_RunSettings()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(UseDataCollectorElement.True));
            var runSettingsFilePath = ".runsettings";
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(runSettingsFilePath);

            var settingsXml = "<settings../>";
            var mockFileUtil = mocker.GetMock<IFileUtil>();
            mockFileUtil.Setup(f => f.ReadAllText(runSettingsFilePath)).Returns(settingsXml);

            var mockRunSettingsCoverletConfiguration = new Mock<IRunSettingsCoverletConfiguration>();
            var mockRunSettingsCoverletConfigurationFactory = mocker.GetMock<IRunSettingsCoverletConfigurationFactory>();
            mockRunSettingsCoverletConfigurationFactory.Setup(rscf => rscf.Create()).Returns(mockRunSettingsCoverletConfiguration.Object);
            coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object);

            mockRunSettingsCoverletConfigurationFactory.VerifyAll();
            mockRunSettingsCoverletConfiguration.Verify(rsc => rsc.Read(settingsXml));

        }

        [TestCase(UseDataCollectorElement.None)]
        [TestCase(UseDataCollectorElement.True)]
        public void Should_Use_Data_Collector_If_RunSettings_Has_The_Data_Collector_Enabled_And_Not_Overridden_By_Project_File(UseDataCollectorElement useDataCollector)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(useDataCollector));
            SetupDataCollectorState(mockCoverageProject, CoverletDataCollectorState.Enabled);
            
            Assert.True(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

        [Test]
        public void Should_Not_Use_Data_Collector_If_RunSettings_Has_The_Data_Collector_Enabled_And_Overridden_By_Project_File()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(UseDataCollectorElement.False));

            SetupDataCollectorState(mockCoverageProject, CoverletDataCollectorState.Enabled);

            Assert.False(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

        [TestCase(UseDataCollectorElement.True)]
        [TestCase(UseDataCollectorElement.Empty)]
        public void Should_Use_Data_Collector_If_Not_Specified_In_RunSettings_And_Specified_In_ProjectFile(UseDataCollectorElement useDataCollectorElement)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(useDataCollectorElement));

            SetupDataCollectorState(mockCoverageProject, CoverletDataCollectorState.NotPresent);

            Assert.True(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

        [Test]
        public void Should_Use_Data_Collector_If_No_RunSettings_And_Specified_In_ProjectFile()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns((string)null);
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(UseDataCollectorElement.True));

            Assert.True(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }


        [TestCase(UseDataCollectorElement.False)]
        [TestCase(UseDataCollectorElement.None)]
        public void Should_Not_Use_Data_Collector_If_Not_Specified_In_RunSettings_And_Not_Specified_In_ProjectFile(UseDataCollectorElement useDataCollector)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(useDataCollector));
            SetupDataCollectorState(mockCoverageProject, CoverletDataCollectorState.NotPresent);
            
            Assert.False(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

        [TestCase(UseDataCollectorElement.False)]
        [TestCase(UseDataCollectorElement.None)]
        public void Should_Not_Use_Data_Collector_If_No_RunSettings_And_Not_Specified_In_ProjectFile(UseDataCollectorElement useDataCollector)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns((string)null);
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(useDataCollector));

            Assert.False(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

        [TestCase(UseDataCollectorElement.True)]
        [TestCase(UseDataCollectorElement.False)]
        [TestCase(UseDataCollectorElement.None)]
        [TestCase(UseDataCollectorElement.Empty)]
        public void Should_Not_Use_Data_Collector_If_Disabled_In_RunSettings(UseDataCollectorElement useDataCollector)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(GetProjectElementWithDataCollector(useDataCollector));
            SetupDataCollectorState(mockCoverageProject, CoverletDataCollectorState.Disabled);

            Assert.False(coverletDataCollectorUtil.CanUseDataCollector(mockCoverageProject.Object));
        }

    }
}