using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Test
{
    public class CoverageProject_Settings_Tests
    {
        //[Test]
        public void Should_Get_Settings_From_CoverageProjectSettingsManager()
        {

        }

    }

    public class CoverageProjectSettingsManager_Tests
    {
        [Test]
        public async Task Should_Use_Global_Settings_If_No_Project_Level()
        {
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            var appOptions = mockAppOptions.Object;
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(appOptions);

            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                mockAppOptionsProvider.Object,
                null,
                new Mock<IVsBuildFCCSettingsProvider>().Object
            );

            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(new XElement("Project"));
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            Assert.AreSame(appOptions, coverageProjectSettings);
        }

        [Test]
        public async Task Should_Prefer_ProjectLevel_From_FCC_Labelled_PropertyGroup_Over_Global()
        {
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            mockAppOptions.SetupSet(o => o.ThresholdForCrapScore = 123); // int type
            mockAppOptions.SetupSet(o => o.CoverletCollectorDirectoryPath = "CoverletCollectorDirectoryPath"); // string type
            mockAppOptions.SetupSet(o => o.IncludeReferencedProjects = true); // bool type
            mockAppOptions.SetupSet(o => o.Exclude = new string[] { "1","2"}); // string array
            var appOptions = mockAppOptions.Object;
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(appOptions);

            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                mockAppOptionsProvider.Object,
                null,
                // does not use if has FineCodeCoverage PropertyGroup with label
                new Mock<IVsBuildFCCSettingsProvider>(MockBehavior.Strict).Object
            );

            var mockCoverageProject = new Mock<ICoverageProject>();
            var projectFileElement = XElement.Parse(@"
<Project>

<PropertyGroup Label='FineCodeCoverage'>
    <ThresholdForCrapScore>123</ThresholdForCrapScore>
    <CoverletCollectorDirectoryPath>CoverletCollectorDirectoryPath</CoverletCollectorDirectoryPath>
    <IncludeReferencedProjects>true</IncludeReferencedProjects>
    <Exclude>
        1
        2
    </Exclude>
</PropertyGroup>
</Project>
");
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(projectFileElement);
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            Assert.AreSame(appOptions, coverageProjectSettings);
            mockAppOptions.VerifyAll();
        }

        [Test]
        public async Task Should_Prefer_ProjectLevel_From_Vs_Build_When_No_FCC_Labelled_PropertyGroup_Over_Global()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            var projectId = Guid.NewGuid();
            mockCoverageProject.Setup(cp => cp.Id).Returns(projectId);

            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            mockAppOptions.SetupSet(o => o.HideFullyCovered = true);
            var appOptions = mockAppOptions.Object;
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(appOptions);

            var mockBuildVsBuildFCCSettingsProvider = new Mock<IVsBuildFCCSettingsProvider>();
            mockBuildVsBuildFCCSettingsProvider.Setup(vsBuildFCCSettingsProvider => vsBuildFCCSettingsProvider.GetSettingsAsync(projectId)).ReturnsAsync(
                XElement.Parse(@"
                    <Container>
                        <HideFullyCovered>true</HideFullyCovered>
                    </Container>
                ")
            );
            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                mockAppOptionsProvider.Object,
                null,
                mockBuildVsBuildFCCSettingsProvider.Object
            );

            
            var projectFileElement = XElement.Parse(@"
<Project>

<PropertyGroup Label='NotFineCodeCoverage'>
    <ThresholdForCrapScore>123</ThresholdForCrapScore>
</PropertyGroup>
</Project>
");
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(projectFileElement);
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            Assert.AreSame(appOptions, coverageProjectSettings);
            mockAppOptions.VerifyAll();
        }
    }
}