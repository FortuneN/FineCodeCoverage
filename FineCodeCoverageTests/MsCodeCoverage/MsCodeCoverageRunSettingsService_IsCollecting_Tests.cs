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

    internal class UserRunSettingsAnalysisResult : IUserRunSettingsAnalysisResult
    {
        public UserRunSettingsAnalysisResult(bool suitable, bool specifiedMsCodeCoverage)
        {
            Suitable = suitable;
            SpecifiedMsCodeCoverage = specifiedMsCodeCoverage;
        }
        public UserRunSettingsAnalysisResult() { }

        public bool Suitable { get; set; }

        public bool SpecifiedMsCodeCoverage { get; set; }

        public List<ICoverageProject> ProjectsWithFCCMsTestAdapter { get; set; } = new List<ICoverageProject>();
    }

    internal class MsCodeCoverageRunSettingsService_IsCollecting_Tests
    {
        private AutoMoqer autoMocker;
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        private const string solutionDirectory = "SolutionDirectory";
        
        

        private class ExceptionReason : IExceptionReason
        {
            public Exception Exception { get; set; }

            public string Reason { get; set; }
        }
        private class ProjectRunSettingsFromTemplateResult : IProjectRunSettingsFromTemplateResult
        {
            public IExceptionReason ExceptionReason { get; set; }

            public List<string> CustomTemplatePaths { get; set; } = new List<string>();

            public List<ICoverageProject> CoverageProjectsWithFCCMsTestAdapter { get; set; } = new List<ICoverageProject>();
        }

        [SetUp]
        public void SetupSut()
        {
            autoMocker = new AutoMoqer();
            msCodeCoverageRunSettingsService = autoMocker.Create<MsCodeCoverageRunSettingsService>();
            msCodeCoverageRunSettingsService.threadHelper = new TestThreadHelper();
            SetupAppOptionsProvider(RunMsCodeCoverage.Yes);
        }

        [Test]
        public async Task Should_Not_Be_Collecting_If_RunMsCodeCoverage_No()
        {
            SetupAppOptionsProvider(RunMsCodeCoverage.No);
            var testOperation = SetUpTestOperation(new List<ICoverageProject> {  });
            var collectionStatus = await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            Assert.AreEqual(MsCodeCoverageCollectionStatus.NotCollecting, collectionStatus);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Try_Analyse_Projects_With_Runsettings(bool useMsCodeCoverageOption)
        {
            var runMsCodeCoverage = useMsCodeCoverageOption ? RunMsCodeCoverage.Yes : RunMsCodeCoverage.IfInRunSettings;
            SetupAppOptionsProvider(runMsCodeCoverage);

            var fccMsTestAdapterPath = InitializeFCCMsTestAdapterPath();

            var coverageProjectWithRunSettings = CreateCoverageProject(".runsettings");
            var templatedCoverageProject = CreateCoverageProject(null);
            var coverageProjects = new List<ICoverageProject> { coverageProjectWithRunSettings, templatedCoverageProject };
            var testOperation = SetUpTestOperation(coverageProjects);

            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            autoMocker.Verify<IUserRunSettingsService>(
                userRunSettingsService => userRunSettingsService.Analyse(
                    new List<ICoverageProject> { coverageProjectWithRunSettings},
                    useMsCodeCoverageOption,
                    fccMsTestAdapterPath)
                );
            
        }

        [Test] // in case shutdown visual studio before normal clean up operation
        public async Task Should_CleanUp_Projects_With_RunSettings_First()
        {
            var coverageProjectWithRunSettings = CreateCoverageProject(".runsettings");
            var coverageProjects = new List<ICoverageProject> { coverageProjectWithRunSettings, CreateCoverageProject(null) };
            var testOperation = SetUpTestOperation(coverageProjects);

            var cleanedUp = false;
            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            mockUserRunSettingsService.Setup(
                userRunSettingsService => userRunSettingsService.Analyse(
                    It.IsAny<IEnumerable<ICoverageProject>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()
                )
            ).Callback(() =>
            {
                Assert.True(cleanedUp);
            });

            var mockTemplatedRunSettingsService = autoMocker.GetMock<ITemplatedRunSettingsService>();
            mockTemplatedRunSettingsService.Setup(
                templatedRunSettingsService => 
                templatedRunSettingsService.CleanUpAsync(new List<ICoverageProject> { coverageProjectWithRunSettings})
            ).Callback(() =>
            {
                cleanedUp = true;
            });
            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            mockUserRunSettingsService.VerifyAll();
        }

        [Test]
        public async Task Should_Log_Exception_From_UserRunSettingsService_Analyse()
        {
            var exception = new Exception("Msg");
            await Throw_Exception_From_UserRunSettingsService_Analyse(exception);
            VerifyLogException("Exception analysing runsettings files", exception);
        }

        [Test]
        public async Task Should_Have_Status_Error_When_Exception_From_UserRunSettingsService_Analyse()
        {
            var exception = new Exception("Msg");
            var status = await Throw_Exception_From_UserRunSettingsService_Analyse(exception);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Error, status);
        }

        [Test]
        public async Task Should_Report_End_Of_CoverageRun_If_Error()
        {
            var exception = new Exception("Msg");
            await Throw_Exception_From_UserRunSettingsService_Analyse(exception);
            autoMocker.Verify<IReportGeneratorUtil>(reportGeneratorUtil => reportGeneratorUtil.EndOfCoverageRun());
        }

        private Task<MsCodeCoverageCollectionStatus> Throw_Exception_From_UserRunSettingsService_Analyse(Exception exception)
        {
            SetupIUserRunSettingsServiceAnalyseAny().Throws(exception);
            return msCodeCoverageRunSettingsService.IsCollectingAsync(SetUpTestOperation());
        }

        [Test]
        public async Task Should_Prepare_Coverage_Projects_When_Suitable()
        {
            SetupAppOptionsProvider(RunMsCodeCoverage.IfInRunSettings);

            var mockTemplatedCoverageProject = new Mock<ICoverageProject>();
            var mockCoverageProjects = new List<Mock<ICoverageProject>>
            {
                mockTemplatedCoverageProject,
                CreateMinimalMockRunSettingsCoverageProject()
        };
            var coverageProjects = mockCoverageProjects.Select(mockCoverageProject => mockCoverageProject.Object).ToList();
            var testOperation = SetUpTestOperation(coverageProjects);

            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, false));

            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            autoMocker.Verify<ICoverageToolOutputManager>(coverageToolOutputManager => coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects));
            foreach (var mockCoverageProject in mockCoverageProjects)
            {
                mockCoverageProject.Verify(coverageProject => coverageProject.PrepareForCoverageAsync(CancellationToken.None, false));
            }
        }

        [Test]
        public async Task Should_Set_UserRunSettingsProjectDetailsLookup_For_IRunSettingsService_When_Suitable()
        {
            SetupAppOptionsProvider(RunMsCodeCoverage.IfInRunSettings);

            var projectSettings = new Mock<IAppOptions>().Object;
            var excludedReferencedProjects = new List<string>();
            var includedReferencedProjects = new List<string>();
            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null),
                CreateCoverageProject(
                    ".runsettings",
                    projectSettings,
                    "OutputFolder",
                    "Test.dll",
                    excludedReferencedProjects, 
                    includedReferencedProjects
                )
            };
            var testOperation = SetUpTestOperation(coverageProjects);

            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, false));
            
            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            var userRunSettingsProjectDetailsLookup = msCodeCoverageRunSettingsService.userRunSettingsProjectDetailsLookup;
            Assert.AreEqual(1, userRunSettingsProjectDetailsLookup.Count);
            var userRunSettingsProjectDetails = userRunSettingsProjectDetailsLookup["Test.dll"];
            Assert.AreSame(projectSettings, userRunSettingsProjectDetails.Settings);
            Assert.AreSame(excludedReferencedProjects, userRunSettingsProjectDetails.ExcludedReferencedProjects);
            Assert.AreSame(includedReferencedProjects, userRunSettingsProjectDetails.IncludedReferencedProjects);
            Assert.AreEqual("OutputFolder", userRunSettingsProjectDetails.CoverageOutputFolder);
            Assert.AreEqual("Test.dll", userRunSettingsProjectDetails.TestDllFile);
        }

        [Test]
        public async Task Should_Be_Collecting_When_Suitable_RunSettings_And_No_Templates()
        {
            var status = await IsCollecting_With_Suitable_RunSettings_Only();
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Collecting, status);
        }

        [Test]
        public async Task Should_Combined_Log_Collecting_With_RunSettings_When_Only_Suitable_RunSettings()
        {
            await IsCollecting_With_Suitable_RunSettings_Only();
            VerifyCombinedLogMessage("Ms code coverage with user runsettings");
        }

        private Task<MsCodeCoverageCollectionStatus> IsCollecting_With_Suitable_RunSettings_Only()
        {
            var testOperation = SetUpTestOperation();
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, false));
            return msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);
        }

        [Test]
        public async Task Should_Not_Be_Collecting_If_User_RunSettings_Are_Not_Suitable()
        {
            var testOperation = SetUpTestOperation();
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult());

            var collectionStatus = await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.NotCollecting, collectionStatus);
        }

        [Test]
        public Task Should_Generate_RunSettings_From_Templates_When_MsCodeCoverage_Option_is_True()
        {
            return GenerateRunSettingsFromTemplate(true, false);
        }

        [Test]
        public Task Should_Generate_RunSettings_From_Templates_When_RunSettings_SpecifiedMsCodeCoverage()
        {
            return GenerateRunSettingsFromTemplate(false, true);
        }

        public async Task GenerateRunSettingsFromTemplate(bool msCodeCoverageOptions, bool runSettingsSpecifiedMsCodeCoverage)
        {
            var runMsCodeCoverage = msCodeCoverageOptions ? RunMsCodeCoverage.Yes : RunMsCodeCoverage.IfInRunSettings;
            SetupAppOptionsProvider(runMsCodeCoverage);
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, runSettingsSpecifiedMsCodeCoverage));

            var fccMsTestAdapterPath = InitializeFCCMsTestAdapterPath();

            var templateCoverageProject = CreateCoverageProject(null);
            var coverageProjects = new List<ICoverageProject>
            {
                CreateMinimalRunSettingsCoverageProject(),
                templateCoverageProject
            };
            var testOperation = SetUpTestOperation(coverageProjects);

            var mockTemplatedRunSettingsService = autoMocker.GetMock<ITemplatedRunSettingsService>();
            mockTemplatedRunSettingsService.Setup(templatedRunSettingsService => templatedRunSettingsService.GenerateAsync(
                    new List<ICoverageProject> { templateCoverageProject },
                    solutionDirectory,
                    fccMsTestAdapterPath
                )).ReturnsAsync(
                new ProjectRunSettingsFromTemplateResult()
            );

            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            mockTemplatedRunSettingsService.VerifyAll();
        }

        [Test]
        public Task Should_Combined_Log_When_Successfully_Generate_RunSettings_From_Templates()
        {
            return Successful_RunSettings_From_Templates_CombinedLog_Test(
                new List<string> { }, 
                new List<string> { "Ms code coverage" }
            );
        }

        [Test]
        public Task Should_Combined_Log_With_Custom_Template_Paths_When_Successfully_Generate_RunSettings_From_Templates()
        {
            return Successful_RunSettings_From_Templates_CombinedLog_Test(
                new List<string> { "Custom path 1", "Custom path 2","Custom path 2" },
                new List<string> { "Ms code coverage - custom template paths","Custom path 1", "Custom path 2"}
            );
        }

        private async Task Successful_RunSettings_From_Templates_CombinedLog_Test(List<string> customTemplatePaths,List<string> expectedLoggerMessages)
        {
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, true));

            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null)

            };
            var testOperation = SetUpTestOperation(coverageProjects);

            SetupTemplatedRunSettingsServiceGenerateAsyncAllIsAny().ReturnsAsync(
                new ProjectRunSettingsFromTemplateResult { CustomTemplatePaths = customTemplatePaths}
            );


            await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);
           
            autoMocker.Verify<ILogger>(logger => logger.Log(expectedLoggerMessages));
            autoMocker.Verify<IReportGeneratorUtil>(reportGeneratorUtil => reportGeneratorUtil.LogCoverageProcess("Ms code coverage"));
        }

        [Test]
        public async Task Should_Combined_Log_Exception_From_Generate_RunSettings_From_Templates()
        {
            var exception = new Exception("The message");
            await ExceptionWhenGenerateRunSettingsFromTemplates(exception);

            VerifyLogException("The reason", exception);
        }

        [Test]
        public async Task Should_Have_Status_Error_When_Exception_From_Generate_RunSettings_From_Templates()
        {
            var status = await ExceptionWhenGenerateRunSettingsFromTemplates(new Exception());
            Assert.AreEqual(MsCodeCoverageCollectionStatus.Error, status);
        }

        private Task<MsCodeCoverageCollectionStatus> ExceptionWhenGenerateRunSettingsFromTemplates(Exception exception)
        {
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, true));

            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null)

            };
            var testOperation = SetUpTestOperation(coverageProjects);

            SetupTemplatedRunSettingsServiceGenerateAsyncAllIsAny().ReturnsAsync(
                new ProjectRunSettingsFromTemplateResult
                {
                    ExceptionReason = new ExceptionReason
                    {
                        Exception = exception,
                        Reason = "The reason"
                    }
                }
            );

            return msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);
        }

        [Test]
        public async Task Should_Not_Be_Collecting_When_Template_Projects_And_Do_Not_Ms_Collect()
        {
            SetupAppOptionsProvider(RunMsCodeCoverage.IfInRunSettings);
            SetupIUserRunSettingsServiceAnalyseAny().Returns(new UserRunSettingsAnalysisResult(true, false));

            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null)

            };
            var testOperation = SetUpTestOperation(coverageProjects);

            var status = await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);
            Assert.AreEqual(MsCodeCoverageCollectionStatus.NotCollecting, status);
        }

        [Test]
        public async Task Should_Shim_Copy_From_RunSettingsProjects_And_Template_Projects_That_Require_It()
        {
            var shimPath = InitializeShimPath();

            var runSettingsProjectsForShim = new List<ICoverageProject>
            {
                CreateCoverageProject(".runsettings")
            };
            SetupIUserRunSettingsServiceAnalyseAny().Returns(
                new UserRunSettingsAnalysisResult
                {
                    Suitable = true,
                    SpecifiedMsCodeCoverage = true,
                    ProjectsWithFCCMsTestAdapter = runSettingsProjectsForShim
                });

            var coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject(null)

            };
            var testOperation = SetUpTestOperation(coverageProjects);

            var templateProjectsForShim = new List<ICoverageProject>
            {
                CreateCoverageProject(null)
            };
            SetupTemplatedRunSettingsServiceGenerateAsyncAllIsAny().ReturnsAsync(
                new ProjectRunSettingsFromTemplateResult
                {
                    CoverageProjectsWithFCCMsTestAdapter = templateProjectsForShim
                }
            );

            var status = await msCodeCoverageRunSettingsService.IsCollectingAsync(testOperation);

            var expectedCoverageProjectsForShimCopy = runSettingsProjectsForShim;
            expectedCoverageProjectsForShimCopy.AddRange(templateProjectsForShim);
            autoMocker.Verify<IShimCopier>(shimCopier => shimCopier.Copy(shimPath, expectedCoverageProjectsForShimCopy));
        }

        private Moq.Language.Flow.ISetup<ITemplatedRunSettingsService, Task<IProjectRunSettingsFromTemplateResult>> SetupTemplatedRunSettingsServiceGenerateAsyncAllIsAny()
        {
            var mockTemplatedRunSettingsService = autoMocker.GetMock<ITemplatedRunSettingsService>();
            return mockTemplatedRunSettingsService.Setup(templatedRunSettingsService => templatedRunSettingsService.GenerateAsync(
                    It.IsAny<IEnumerable<ICoverageProject>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ));
        }

        private Moq.Language.Flow.ISetup<IUserRunSettingsService, IUserRunSettingsAnalysisResult> SetupIUserRunSettingsServiceAnalyseAny()
        {
            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            return mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.Analyse(
                It.IsAny<List<ICoverageProject>>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ));
        }

        private ITestOperation SetUpTestOperation(List<ICoverageProject> coverageProjects = null)
        {
            coverageProjects = coverageProjects ?? new List<ICoverageProject>();
            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(coverageProjects);
            mockTestOperation.Setup(testOperation => testOperation.SolutionDirectory).Returns(solutionDirectory);
            return mockTestOperation.Object;
        }

        private void SetupAppOptionsProvider(RunMsCodeCoverage runMsCodeCoverage)
        {
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            var mockOptions = new Mock<IAppOptions>();
            mockOptions.Setup(options => options.RunMsCodeCoverage).Returns(runMsCodeCoverage);
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(mockOptions.Object);
        }

        private void VerifyLogException(string reason, Exception exception)
        {
            autoMocker.Verify<ILogger>(l => l.Log(reason, exception.ToString()));
            autoMocker.Verify<IReportGeneratorUtil>(reportGenerator => reportGenerator.LogCoverageProcess(reason));
        }

        private void VerifyCombinedLogMessage(string message)
        {
            autoMocker.Verify<ILogger>(l => l.Log(message));
            autoMocker.Verify<IReportGeneratorUtil>(reportGenerator => reportGenerator.LogCoverageProcess(message));
        }

        private string InitializeFCCMsTestAdapterPath()
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

        private ICoverageProject CreateCoverageProject(
            string runSettingsFile,
            IAppOptions settings = null,
            string coverageOutputFolder = "",
            string testDllFile = "", 
            List<string> excludedReferencedProjects = null,
            List<string> includedReferencedProjects = null,
            string projectFile = ""
        )
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(runSettingsFile);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns(coverageOutputFolder);
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns(testDllFile);
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(excludedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(includedReferencedProjects);
            mockCoverageProject.Setup(cp => cp.Settings).Returns(settings);
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns(projectFile);
            return mockCoverageProject.Object;
        }

        private Mock<ICoverageProject> CreateMinimalMockRunSettingsCoverageProject()
        {
            var mockCoverageProjectWithRunSettings = new Mock<ICoverageProject>();
            mockCoverageProjectWithRunSettings.Setup(cp => cp.RunSettingsFile).Returns(".runsettings");
            mockCoverageProjectWithRunSettings.Setup(cp => cp.TestDllFile).Returns("Test.dll");
            return mockCoverageProjectWithRunSettings;
        }

        private ICoverageProject CreateMinimalRunSettingsCoverageProject()
        {
            return CreateMinimalMockRunSettingsCoverageProject().Object;
        }
    }
}
