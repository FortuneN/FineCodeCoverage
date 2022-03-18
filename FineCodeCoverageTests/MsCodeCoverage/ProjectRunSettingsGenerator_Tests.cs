using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using AutoMoq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using FineCodeCoverage.Engine.Model;
using System;
using FineCodeCoverage.Core.Utilities;
using System.IO;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class TestCoverageProjectRunSettings : ICoverageProjectRunSettings
    {
        public TestCoverageProjectRunSettings(Guid id,string outputFolder, string projectName,string runSettings)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.Id).Returns(id);
            mockCoverageProject.Setup(cp => cp.ProjectName).Returns(projectName);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns(outputFolder);
            CoverageProject = mockCoverageProject.Object;

            RunSettings = runSettings;
        }
        public ICoverageProject CoverageProject { get; set; }
        public string RunSettings { get; set; }
    }

    internal class ProjectRunSettingsGenerator_Tests
    {
        private AutoMoqer autoMocker;
        private ProjectRunSettingsGenerator projectRunSettingsGenerator;
        private Guid projectId1;
        private Guid projectId2;
        private List<ICoverageProjectRunSettings> coverageProjectsRunSettings;
        private string generatedRunSettingsInOutputFolderPath1;
        private string generatedRunSettingsInOutputFolderPath2;

        [SetUp]
        public void Setup()
        {
            autoMocker = new AutoMoqer();
            projectRunSettingsGenerator = autoMocker.Create<ProjectRunSettingsGenerator>();
            projectId1 = Guid.NewGuid();
            projectId2 = Guid.NewGuid();
            coverageProjectsRunSettings = new List<ICoverageProjectRunSettings>
            {
                new TestCoverageProjectRunSettings(projectId1, "OutputFolder1","Project1","RunSettings1"),
                new TestCoverageProjectRunSettings(projectId2, "OutputFolder2","Project2","RunSettings2"),
            };
            generatedRunSettingsInOutputFolderPath1 = Path.Combine("OutputFolder1", "Project1-fcc-mscodecoverage-generated.runsettings");
            generatedRunSettingsInOutputFolderPath2 = Path.Combine("OutputFolder2", "Project2-fcc-mscodecoverage-generated.runsettings");
        }

        [Test]
        public async Task Should_Write_All_Project_Run_Settings_File_Path_With_The_VsRunSettingsWriter()
        {
            await projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(coverageProjectsRunSettings);

            var mockVsRunSettingsWriter = autoMocker.GetMock<IVsRunSettingsWriter>();
            
            mockVsRunSettingsWriter.Verify(rsw => rsw.WriteRunSettingsFilePathAsync(projectId1, generatedRunSettingsInOutputFolderPath1 ));
            mockVsRunSettingsWriter.Verify(rsw => rsw.WriteRunSettingsFilePathAsync(projectId2, generatedRunSettingsInOutputFolderPath2 ));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Write_RunSettings_In_Project_Output_Folder_If_The_VsRunSettingsWriter_Is_Successful(bool success)
        {
            var mockVsRunSettingsWriter = autoMocker.GetMock<IVsRunSettingsWriter>();
            mockVsRunSettingsWriter.Setup(rsw => rsw.WriteRunSettingsFilePathAsync(projectId1, generatedRunSettingsInOutputFolderPath1)).ReturnsAsync(success);
            mockVsRunSettingsWriter.Setup(rsw => rsw.WriteRunSettingsFilePathAsync(projectId2, generatedRunSettingsInOutputFolderPath2)).ReturnsAsync(success);

            var mockFileUtil = autoMocker.GetMock<IFileUtil>();
            await projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(coverageProjectsRunSettings);
            if (success)
            {
                mockFileUtil.Verify(f => f.WriteAllText(generatedRunSettingsInOutputFolderPath1, "RunSettings1"));
                mockFileUtil.Verify(f => f.WriteAllText(generatedRunSettingsInOutputFolderPath2, "RunSettings2"));
            }
            else
            {
                mockFileUtil.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }

        [Test]
        public async Task Should_Remove_Generated_Run_Settings_File_Path_With_The_VsRunSettingsWriter()
        {
            var mockProjectWithGeneratedRunSettings = new Mock<ICoverageProject>();
            var mockProjectWithoutRunSettings = new Mock<ICoverageProject>();
            var mockProjectWithoutGeneratedRunSettings = new Mock<ICoverageProject>();
            

            ICoverageProject SetupProject(Mock<ICoverageProject> mockCoverageProject, string runSettingsFilePath, Guid id)
            {
                mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(runSettingsFilePath);
                mockCoverageProject.Setup(cp => cp.Id).Returns(id);
                return mockCoverageProject.Object;
            }

            var p1 = SetupProject(mockProjectWithGeneratedRunSettings, "Project1-fcc-mscodecoverage-generated.runsettings", projectId1);
            var p2 = SetupProject(mockProjectWithoutRunSettings,null, projectId2);
            var p3 = SetupProject(mockProjectWithoutGeneratedRunSettings, "", Guid.NewGuid());

            await projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(new ICoverageProject[] { p1, p2, p3 });

            var mockVsRunSettingsWriter = autoMocker.GetMock<IVsRunSettingsWriter>();
            mockVsRunSettingsWriter.Verify(rsw => rsw.RemoveRunSettingsFilePathAsync(projectId1));
            mockVsRunSettingsWriter.VerifyNoOtherCalls();
        }
    }
}
