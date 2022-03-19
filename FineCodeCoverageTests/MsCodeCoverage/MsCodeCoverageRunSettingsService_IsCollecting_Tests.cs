using Moq;
using NUnit.Framework;
using AutoMoq;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverage.Impl;
using System.Threading.Tasks;
using FineCodeCoverage.Options;
using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;
using FineCodeCoverage.Engine;
using System.Linq;
using System.Threading;
using FineCodeCoverageTests.Test_helpers;
using System.Xml.Linq;
using System;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverageRunSettingsService_IsCollecting_Tests
    {
        private AutoMoqer autoMocker;
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;

        [SetUp]
        public void SetupSut()
        {
            autoMocker = new AutoMoqer();
            msCodeCoverageRunSettingsService = autoMocker.Create<MsCodeCoverageRunSettingsService>();
            msCodeCoverageRunSettingsService.threadHelper = new TestThreadHelper();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Not_Be_Collecting_If_User_RunSettings_Are_Not_Suitable(bool useMsCodeCoverage)
        {
            SetupAppOptionsProvider(useMsCodeCoverage);

            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(new List<ICoverageProject>
            {
                CreateCoverageProject("RunSettings1"),
                CreateCoverageProject("RunSettings2"),
                CreateCoverageProject(null),
            });

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(new List<string>
            {
                "RunSettings1",
                "RunSettings2",
            }, useMsCodeCoverage)).Returns((false, false));

            var collectionStatus = await msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.NotCollecting, collectionStatus);
            mockUserRunSettingsService.VerifyAll();
        }

        private void SetupAppOptionsProvider(bool useMsCodeCoverage = true)
        {
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            var mockOptions = new Mock<IAppOptions>();
            mockOptions.Setup(options => options.MsCodeCoverage).Returns(useMsCodeCoverage);
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(mockOptions.Object);
        }

        [Test]
        public async Task Should_Prepare_Coverage_Projects_When_Suitable()
        {
            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();
            var mockCoverageProjects = new List<Mock<ICoverageProject>>
            {
                new Mock<ICoverageProject>(),
                new Mock<ICoverageProject>(),
            };
            var coverageProjects = mockCoverageProjects.Select(mockCoverageProject => mockCoverageProject.Object).ToList();
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true,false));

            await msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);
            
            autoMocker.Verify<ICoverageToolOutputManager>(coverageToolOutputManager => coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects));
            foreach(var mockCoverageProject in mockCoverageProjects)
            {
                mockCoverageProject.Verify(coverageProject => coverageProject.PrepareForCoverageAsync(CancellationToken.None, false));
            }
        }

        [Test]
        public async Task Should_Set_UserRunSettingsProjectDetailsLookup_For_IRunSettingsService_When_Suitable()
        {
            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();

            var projectSettings = new Mock<IAppOptions>().Object;
            var excludedReferencedProjects = new List<string>();
            var includedReferencedProjects = new List<string>();
            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null),
                CreateCoverageProject("rs",projectSettings,"OutputFolder","Test.dll",excludedReferencedProjects, includedReferencedProjects)
            };
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, false));

            await msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);

            var userRunSettingsProjectDetailsLookup = msCodeCoverageRunSettingsService.userRunSettingsProjectDetailsLookup;
            Assert.AreEqual(1, userRunSettingsProjectDetailsLookup.Count);
            var userRunSettingsProjectDetails = userRunSettingsProjectDetailsLookup["Test.dll"];
            Assert.AreSame(projectSettings, userRunSettingsProjectDetails.Settings);
            Assert.AreSame(excludedReferencedProjects, userRunSettingsProjectDetails.ExcludedReferencedProjects);
            Assert.AreSame(includedReferencedProjects, userRunSettingsProjectDetails.IncludedReferencedProjects);
            Assert.AreEqual("OutputFolder", userRunSettingsProjectDetails.OutputFolder);
            Assert.AreEqual("Test.dll", userRunSettingsProjectDetails.TestDllFile);
        }


        private ICoverageProject CreateCoverageProject(string runSettingsFile,IAppOptions settings = null,string outputFolder = "",string testDllFile = "", List<string> excludedReferencedProjects = null,List<string> includedReferencedProjects = null)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(runSettingsFile);
            mockCoverageProject.Setup(cp => cp.ProjectOutputFolder).Returns(outputFolder);
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns(testDllFile);
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(excludedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(includedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.Settings).Returns(settings);
            return mockCoverageProject.Object;
        }
    }
}
