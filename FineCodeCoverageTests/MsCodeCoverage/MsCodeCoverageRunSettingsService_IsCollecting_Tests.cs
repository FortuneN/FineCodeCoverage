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
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Core.Utilities;
using System.IO;
using System;

namespace FineCodeCoverageTests.MsCodeCoverage
{

    internal class MsCodeCoverageRunSettingsService_StopCoverage_Test
    {
        [Test]
        public void Should_StopCoverage_On_FCCEngine()
        {
            var autoMocker = new AutoMoqer();

            var mockToolFolder = autoMocker.GetMock<IToolFolder>();
            mockToolFolder.Setup(tf => tf.EnsureUnzipped(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ZipDetails>(), It.IsAny<CancellationToken>())).Returns("ZipDestination");

            var msCodeCoverageRunSettingsService = autoMocker.Create<MsCodeCoverageRunSettingsService>();
            var mockFccEngine = new Mock<IFCCEngine>();

            msCodeCoverageRunSettingsService.Initialize(null, mockFccEngine.Object, CancellationToken.None);

            msCodeCoverageRunSettingsService.StopCoverage();
            mockFccEngine.Verify(fccEngine => fccEngine.StopCoverage());
        }
    }

    internal class MsCodeCoverageRunSettingsService_IsCollecting_Tests
    {
        private AutoMoqer autoMocker;
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private const string solutionDirectory = "SolutionDirectory";

        private class TemplateReplaceResult : ITemplateReplaceResult
        {
            public string Replaced { get; set; }

            public bool ReplacedTestAdapter { get; set; }
        }

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

        [Test]
        public async Task Should_Be_Collecting_When_All_Projects_Have_Suitable_RunSettings()
        {
            var msCodeCoverageCollectionStatus = await IsCollectingWhenAllProjectsHaveSuitableRunSettings();
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Collecting, msCodeCoverageCollectionStatus);
        }

        [Test]
        public async Task Should_Combined_Log_When_All_Projects_Have_Suitable_RunSettings()
        {
            await IsCollectingWhenAllProjectsHaveSuitableRunSettings();
            var expectedMessage = "Ms code coverage with user runsettings";
            autoMocker.Verify<ILogger>(l => l.Log(expectedMessage));
            autoMocker.Verify<IReportGeneratorUtil>(rg => rg.LogCoverageProcess(expectedMessage));
        }

        [Test]
        public async Task Should_Be_Collecting_When_Suitable_RunSettings_Specified_Ms_Data_Collector_And_Projects_Without_RunSettings()
        {
            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = "<Root/>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>())).Returns(templateReplaceResult);
            var coverageProjects = new List<ICoverageProject>
            {
                new Mock<ICoverageProject>().Object
            };
            var msCodeCoverageCollectionStatus = await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Collecting, msCodeCoverageCollectionStatus);
        }

        [Test]
        public async Task Should_Be_Collecting_When_Suitable_RunSettings_Did_Not_Specify_Ms_Data_Collector_And_Projects_Without_RunSettings_And_Use_MsCodeCoverage()
        {
            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = "<Root/>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>())).Returns(templateReplaceResult);
            var coverageProjects = new List<ICoverageProject>
            {
                new Mock<ICoverageProject>().Object
            };
            var msCodeCoverageCollectionStatus = await IsCollectingWhenAllProjectsHaveSuitableRunSettings(true, coverageProjects, false);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Collecting, msCodeCoverageCollectionStatus);
        }

        [Test]
        public async Task Should_Not_be_Collecting__When_Suitable_RunSettings_Did_Not_Specify_Ms_Data_Collector_And_Projects_Without_RunSettings_And_Do_Not_Use_MsCodeCoverage()
        {
            var coverageProjects = new List<ICoverageProject>
            {
                new Mock<ICoverageProject>().Object
            };
            var msCodeCoverageCollectionStatus = await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, false);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.NotCollecting, msCodeCoverageCollectionStatus);
        }

        [Test]
        public async Task Should_Shim_Copy_Projects_Without_RunSettings_With_Replaced_Test_Adapter_When_Suitable()
        {
            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResultDidNotReplace = new TemplateReplaceResult
            {
                Replaced = "<Root/>",
            };
            var templateReplaceResultReplaced = new TemplateReplaceResult
            {
                Replaced = "<Root/>",
                ReplacedTestAdapter = true
            };
            mockBuiltInRunSettingsTemplate.SetupSequence(
                birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>()))
                .Returns(templateReplaceResultDidNotReplace)
                .Returns(templateReplaceResultReplaced);


            var shimPath = InitializeShimPath();

            var coverageProjectWithRunSettings = CreateCoverageProject("runsettings");
            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null,@"SomeProject\SomeProject.csproj");
            var coverageProjectWithoutRunSettingsReplacedTestAdapter = CreateCoverageProject(null, null, null, null, null,null, @"SomeProject2\SomeProject2.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                coverageProjectWithRunSettings,
                coverageProjectWithoutRunSettings,
                coverageProjectWithoutRunSettingsReplacedTestAdapter
            };
            var msCodeCoverageCollectionStatus = await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);


            autoMocker.Verify<IShimCopier>(shimCopier => shimCopier.Copy(shimPath, new List<ICoverageProject> { coverageProjectWithoutRunSettingsReplacedTestAdapter }));
        }

        [Test]
        public async Task Should_Write_Pretty_Replaced_Configured_Custom_Templates_When_Available()
        {
            var coverageProjectWithRunSettings = CreateCoverageProject("runsettings");
            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                coverageProjectWithRunSettings,
                coverageProjectWithoutRunSettings
            };

            var runSettingsTemplateReplacements = new Mock<IRunSettingsTemplateReplacements>().Object;
            var testAdapter = InitializeMsCodeCoverageTestAdapterPath();
            autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>()
                .Setup(runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(coverageProjectWithoutRunSettings, testAdapter))
                .Returns(runSettingsTemplateReplacements);


            var mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            var customRunSettingsTemplateDetails = new CustomRunSettingsTemplateDetails
            {
                Path = "Custom path",
                Template = "Custom Template"
            };
            mockCustomRunSettingsTemplateProvider.Setup(customRunSettingsProvider => customRunSettingsProvider.Provide("SomeProject", solutionDirectory))
                .Returns(customRunSettingsTemplateDetails);

            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = @"<RunConfiguration>
                                <ResultsDirectory>ProjectOutputFolder</ResultsDirectory>
</RunConfiguration>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.ConfigureCustom("Custom Template")).Returns("Configured Custom Template");
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace("Configured Custom Template", runSettingsTemplateReplacements)).Returns(templateReplaceResult);
            
            await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);

            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            var coverageProjectRunSettings = (mockProjectRunSettingsGenerator.Invocations[0].Arguments[0] as IEnumerable<ICoverageProjectRunSettings>).First();
            Assert.AreSame(coverageProjectWithoutRunSettings, coverageProjectRunSettings.CoverageProject);

            var expectedRunSettings = XDocument.Parse(templateReplaceResult.Replaced).FormatXml();
            Assert.AreEqual(expectedRunSettings, coverageProjectRunSettings.RunSettings);
        }

        [Test]
        public async Task Should_Log_When_Using_Custom_Template()
        {
            var coverageProjectWithRunSettings = CreateCoverageProject("runsettings");
            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                coverageProjectWithRunSettings,
                coverageProjectWithoutRunSettings
            };

            var runSettingsTemplateReplacements = new Mock<IRunSettingsTemplateReplacements>().Object;
            var testAdapter = InitializeMsCodeCoverageTestAdapterPath();
            autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>()
                .Setup(runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(coverageProjectWithoutRunSettings, testAdapter))
                .Returns(runSettingsTemplateReplacements);


            var mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            var customRunSettingsTemplateDetails = new CustomRunSettingsTemplateDetails
            {
                Path = "Custom path",
                Template = "Custom Template"
            };
            mockCustomRunSettingsTemplateProvider.Setup(customRunSettingsProvider => customRunSettingsProvider.Provide("SomeProject", solutionDirectory))
                .Returns(customRunSettingsTemplateDetails);

            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = @"<RunConfiguration>
                                <ResultsDirectory>ProjectOutputFolder</ResultsDirectory>
</RunConfiguration>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.ConfigureCustom("Custom Template")).Returns("Configured Custom Template");
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace("Configured Custom Template", runSettingsTemplateReplacements)).Returns(templateReplaceResult);

            await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);

            autoMocker.Verify<ILogger>(logger => logger.Log(new List<string> { "Ms code coverage - custom template paths", "Custom path" }));
            autoMocker.Verify<IReportGeneratorUtil>(reportGeneratorUtil => reportGeneratorUtil.LogCoverageProcess("Ms code coverage"));
        }

        [Test]
        public async Task Should_Write_Pretty_Replaced_Default_Template_When_No_Custom()
        {
            var coverageProjectWithRunSettings = CreateCoverageProject("runsettings");
            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                coverageProjectWithRunSettings,
                coverageProjectWithoutRunSettings
            };

            var runSettingsTemplateReplacements = new Mock<IRunSettingsTemplateReplacements>().Object;
            var testAdapter = InitializeMsCodeCoverageTestAdapterPath();
            autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>()
                .Setup(runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(coverageProjectWithoutRunSettings, testAdapter))
                .Returns(runSettingsTemplateReplacements);


            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = @"<RunConfiguration>
                                <ResultsDirectory>ProjectOutputFolder</ResultsDirectory>
</RunConfiguration>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Template).Returns("Default template");
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace("Default template", runSettingsTemplateReplacements)).Returns(templateReplaceResult);

            await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);

            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            var coverageProjectRunSettings = (mockProjectRunSettingsGenerator.Invocations[0].Arguments[0] as IEnumerable<ICoverageProjectRunSettings>).First();
            Assert.AreSame(coverageProjectWithoutRunSettings, coverageProjectRunSettings.CoverageProject);

            var expectedRunSettings = XDocument.Parse(templateReplaceResult.Replaced).FormatXml();
            Assert.AreEqual(expectedRunSettings, coverageProjectRunSettings.RunSettings);
        }

        [Test]
        public async Task Should_Log_When_Using_Default_Template()
        {
            var coverageProjectWithRunSettings = CreateCoverageProject("runsettings");
            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                coverageProjectWithRunSettings,
                coverageProjectWithoutRunSettings
            };

            var runSettingsTemplateReplacements = new Mock<IRunSettingsTemplateReplacements>().Object;
            var testAdapter = InitializeMsCodeCoverageTestAdapterPath();
            autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>()
                .Setup(runSettingsTemplateReplacementsFactory => runSettingsTemplateReplacementsFactory.Create(coverageProjectWithoutRunSettings, testAdapter))
                .Returns(runSettingsTemplateReplacements);


            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var templateReplaceResult = new TemplateReplaceResult
            {
                Replaced = @"<RunConfiguration>
                                <ResultsDirectory>ProjectOutputFolder</ResultsDirectory>
</RunConfiguration>",
            };
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Template).Returns("Default template");
            mockBuiltInRunSettingsTemplate.Setup(birst => birst.Replace("Default template", runSettingsTemplateReplacements)).Returns(templateReplaceResult);

            await IsCollectingWhenAllProjectsHaveSuitableRunSettings(false, coverageProjects, true);

            autoMocker.Verify<ILogger>(logger => logger.Log(new List<string> { "Ms code coverage" }));
            autoMocker.Verify<IReportGeneratorUtil>(reportGeneratorUtil => reportGeneratorUtil.LogCoverageProcess("Ms code coverage"));
        }

        [Test]
        public async Task Should_Have_Status_Error_If_Exception_Getting_The_Projects_RunSettings()
        {
            var status = await ThrowGettingProjectRunSettings(new Exception());
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Error, status);
        }

        [Test]
        public async Task Should_Log_If_Exception_Getting_The_Projects_RunSettings()
        {
            var exception = new Exception("The error message");
            await ThrowGettingProjectRunSettings(exception);
            VerifyLogException("Exception generating ms runsettings", exception);
        }

        private void VerifyLogException(string reason, Exception exception)
        {
            autoMocker.Verify<ILogger>(l => l.Log(reason, exception.ToString()));
            autoMocker.Verify<IReportGeneratorUtil>(reportGenerator => reportGenerator.LogCoverageProcess(reason));
        }

        private Task<MsCodeCoverageCollectionStatus> ThrowGettingProjectRunSettings(Exception exception)
        {
            var mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            
            mockCustomRunSettingsTemplateProvider.Setup(thrower => thrower.Provide(It.IsAny<string>(), It.IsAny<string>())).Throws(exception);


            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);

            var coverageProjects = new List<ICoverageProject> { CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj") };
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, false));

            return msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);
        }

        [Test]
        public async Task Should_Have_Status_Error_If_Exception_Writing_The_Project_RunSettings()
        {
            var status = await ThrowWritingProjectRunSettings(new Exception());
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Error, status);
        }

        [Test]
        public async Task Should_Log_If_Exception_Writing_The_Project_RunSettings()
        {
            var exception = new Exception("The message");
            await ThrowWritingProjectRunSettings(exception);
            VerifyLogException("Exception writing ms runsettings",exception);

        }

        private Task<MsCodeCoverageCollectionStatus> ThrowWritingProjectRunSettings(Exception exception)
        {
            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            mockProjectRunSettingsGenerator.Setup(thrower => thrower.WriteProjectsRunSettingsAsync(It.IsAny<IEnumerable<ICoverageProjectRunSettings>>())).Throws(exception);

            var builtInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var replaceResult = new TemplateReplaceResult
            {
                Replaced = "<Root/>"
            };
            builtInRunSettingsTemplate.Setup(birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>())).Returns(replaceResult);

            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);

            var coverageProjects = new List<ICoverageProject> { CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj") };
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, false));

            return msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);
        }

        
        [Test]
        public async Task Should_Remove_Generated_Project_Settings_If_Exception_Writing_Them()
        {
            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            mockProjectRunSettingsGenerator.Setup(thrower => thrower.WriteProjectsRunSettingsAsync(It.IsAny<IEnumerable<ICoverageProjectRunSettings>>())).Throws(new Exception());

            var builtInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var replaceResult = new TemplateReplaceResult
            {
                Replaced = "<Root/>"
            };
            builtInRunSettingsTemplate.Setup(birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>())).Returns(replaceResult);

            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);

            var coverageProjectWithoutRunSettings = CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj");
            var coverageProjects = new List<ICoverageProject> { 
                CreateCoverageProject(""),
                coverageProjectWithoutRunSettings
            };
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, false));

            await msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);

            mockProjectRunSettingsGenerator.Verify(runSettingsGenerator => runSettingsGenerator.RemoveGeneratedProjectSettingsAsync(new List<ICoverageProject> { coverageProjectWithoutRunSettings }));
        }

        [Test]
        public void Should_Swallow_Exception_Removing_Generated_RunSettings()
        {
            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            mockProjectRunSettingsGenerator.Setup(thrower => thrower.WriteProjectsRunSettingsAsync(It.IsAny<IEnumerable<ICoverageProjectRunSettings>>())).Throws(new Exception());
            mockProjectRunSettingsGenerator.Setup(thrower => thrower.RemoveGeneratedProjectSettingsAsync(It.IsAny<IEnumerable<ICoverageProject>>())).Throws(new Exception());

            var builtInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            var replaceResult = new TemplateReplaceResult
            {
                Replaced = "<Root/>"
            };
            builtInRunSettingsTemplate.Setup(birst => birst.Replace(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>())).Returns(replaceResult);

            SetupAppOptionsProvider();
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);

            var coverageProjects = new List<ICoverageProject> {
                CreateCoverageProject(null, null, null, null, null, null, @"SomeProject\SomeProject.csproj")
            };
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, false));

            Assert.DoesNotThrowAsync(() => msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object));
        }


        private string InitializeMsCodeCoverageTestAdapterPath()
        {
            InitializeZipDestination();
            return Path.Combine("ZipDestination", "build", "netstandard1.0");
        }

        private string InitializeShimPath()
        {
            InitializeZipDestination();
            return Path.Combine("ZipDestination", "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
        }

        private void InitializeZipDestination()
        {
            var mockToolFolder = autoMocker.GetMock<IToolFolder>();
            mockToolFolder.Setup(tf => tf.EnsureUnzipped(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ZipDetails>(), It.IsAny<CancellationToken>())).Returns("ZipDestination");
            msCodeCoverageRunSettingsService.Initialize(null, null, CancellationToken.None);
        }

        private Task<MsCodeCoverageCollectionStatus> IsCollectingWhenAllProjectsHaveSuitableRunSettings(bool useMsCodeCoverage = true,List<ICoverageProject> coverageProjects = null,bool specifiedMsDataCollector = false)
        {
            SetupAppOptionsProvider(useMsCodeCoverage);
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);

            coverageProjects = coverageProjects ?? new List<ICoverageProject>();
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.CheckUserRunSettingsSuitability(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>())).Returns((true, specifiedMsDataCollector));

            return msCodeCoverageRunSettingsService.IsCollectingAsync(mockTestOperation.Object);
        }


        private ICoverageProject CreateCoverageProject(
            string runSettingsFile,
            IAppOptions settings = null,
            string outputFolder = "",
            string testDllFile = "", 
            List<string> excludedReferencedProjects = null,
            List<string> includedReferencedProjects = null,
            string projectFile = ""
            )
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(runSettingsFile);
            mockCoverageProject.Setup(cp => cp.ProjectOutputFolder).Returns(outputFolder);
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns(testDllFile);
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(excludedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(includedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.Settings).Returns(settings);
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns(projectFile);
            return mockCoverageProject.Object;
        }
    }
}
