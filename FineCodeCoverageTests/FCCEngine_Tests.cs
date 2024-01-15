using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Options;
using FineCodeCoverage.Output;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class FCCEngine_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;
        private bool updatedMarginTags;

        [SetUp]
        public void SetUp()
        {
            updatedMarginTags = false;
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();
        }

        [Test]
        public void Should_Initialize_AppFolder_Then_Utils()
        {
            var disposalToken = CancellationToken.None;
            List<int> callOrder = new List<int>();

            var appDataFolderPath = "some path";
            var mockAppDataFolder = mocker.GetMock<IAppDataFolder>();
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.Initialize(disposalToken)).Callback(() => callOrder.Add(1));
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.DirectoryPath).Returns(appDataFolderPath);

            var reportGeneratorMock = mocker.GetMock<IReportGeneratorUtil>().Setup(reportGenerator => reportGenerator.Initialize(appDataFolderPath, disposalToken)).Callback(() => callOrder.Add(2));

            var msTestPlatformMock = mocker.GetMock<IMsTestPlatformUtil>().Setup(msTestPlatform => msTestPlatform.Initialize(appDataFolderPath, disposalToken)).Callback(() => callOrder.Add(3));

            var openCoverMock = mocker.GetMock<ICoverageUtilManager>().Setup(openCover => openCover.Initialize(appDataFolderPath, disposalToken)).Callback(() => callOrder.Add(4));

            fccEngine.Initialize(disposalToken);

            Assert.AreEqual(4, callOrder.Count);
            Assert.AreEqual(1, callOrder[0]);
        }

        
        [Test]
        public void Should_Set_AppDataFolderPath_From_Initialized_AppDataFolder_DirectoryPath()
        {
            var appDataFolderPath = "some path";
            var mockAppDataFolder = mocker.GetMock<IAppDataFolder>();
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.DirectoryPath).Returns(appDataFolderPath);
            fccEngine.Initialize(CancellationToken.None);
            Assert.AreEqual("some path", fccEngine.AppDataFolderPath);
        }

        [Test]
        public void Should_Send_NewCoverageLinesMessage_With_Null_CoverageLines_When_ClearUI()
        {
            fccEngine.ClearUI();
            mocker.Verify<IEventAggregator>(ea => ea.SendMessage(It.Is<NewCoverageLinesMessage>(msg => msg.CoverageLines == null), null));
        }

    }

    public class FCCEngine_ReloadCoverage_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            var mockDisposeAwareTaskRunner = mocker.GetMock<IDisposeAwareTaskRunner>();
            mockDisposeAwareTaskRunner.Setup(runner => runner.RunAsync(It.IsAny<Func<Task>>())).Callback<Func<Task>>(async taskProvider => await taskProvider());
            fccEngine = mocker.Create<FCCEngine>();

            var mockedAppOptions = mocker.GetMock<IAppOptions>();
            mockedAppOptions.Setup(x => x.RunMsCodeCoverage).Returns(RunMsCodeCoverage.No);
            var mockAppOptionsProvider = mocker.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(x => x.Get()).Returns(mockedAppOptions.Object);
        }

        [Test]
        public async Task Should_Log_Starting_When_Initialized()
        {
            await ReloadInitializedCoverage();
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Start);
        }

        [Test]
        public async Task Should_Prepare_For_Coverage_Suitable_CoverageProjects()
        {
            var mockSuitableCoverageProject = await ReloadSuitableCoverageProject();
            mockSuitableCoverageProject.Verify(p => p.PrepareForCoverageAsync(It.IsAny<CancellationToken>(),true));
        }

        [Test]
        public async Task Should_Set_Failure_Description_For_Unsuitable_Projects()
        {
            SetUpSuccessfulRunReportGenerator();

            var mockNullProjectFileProject = new Mock<ICoverageProject>();
            mockNullProjectFileProject.Setup(p => p.TestDllFile).Returns("Null_Project_File.dll");
            var mockWhitespaceProjectFileProject = new Mock<ICoverageProject>();
            mockWhitespaceProjectFileProject.Setup(p => p.ProjectFile).Returns("  ");
            mockWhitespaceProjectFileProject.Setup(p => p.TestDllFile).Returns("Whitespace_Project_File.dll");
            var mockDisabledProject = new Mock<ICoverageProject>();
            mockDisabledProject.Setup(p => p.ProjectFile).Returns("proj.csproj");
            mockDisabledProject.Setup(p => p.Settings.Enabled).Returns(false);
            
            await ReloadInitializedCoverage(mockNullProjectFileProject.Object, mockWhitespaceProjectFileProject.Object, mockDisabledProject.Object);
            
            mockDisabledProject.VerifySet(p => p.FailureDescription = "Disabled");
            mockWhitespaceProjectFileProject.VerifySet(p => p.FailureDescription = "Unsupported project type for DLL 'Whitespace_Project_File.dll'");
            mockNullProjectFileProject.VerifySet(p => p.FailureDescription = "Unsupported project type for DLL 'Null_Project_File.dll'");
            
        }

        [Test]
        public async Task Should_Run_The_CoverTool_Step()
        {
            var mockCoverageProject = await ReloadSuitableCoverageProject();
            mockCoverageProject.Verify(p => p.StepAsync("Run Coverage Tool", It.IsAny<Func<ICoverageProject, Task>>()));
        }

        [Test]
        public async Task Should_Run_Coverage_ThrowingErrors_But_Safely_With_StepAsync()
        {
            ICoverageProject coverageProject = null;
            await ReloadSuitableCoverageProject(mockCoverageProject => {
                coverageProject = mockCoverageProject.Object;
                mockCoverageProject.Setup(p => p.StepAsync("Run Coverage Tool", It.IsAny<Func<ICoverageProject, Task>>())).Callback<string,Func<ICoverageProject, Task>>((_,runCoverTool) =>
                {
                    runCoverTool(coverageProject);
                });
            });

            mocker.Verify<ICoverageUtilManager>(coverageUtilManager => coverageUtilManager.RunCoverageAsync(coverageProject, It.IsAny<CancellationToken>()));

        }

        [Test]
        public async Task Should_Allow_The_CoverageOutputManager_To_SetProjectCoverageOutputFolder()
        {
            var mockCoverageToolOutputManager = mocker.GetMock<ICoverageToolOutputManager>();
            mockCoverageToolOutputManager.Setup(om => om.SetProjectCoverageOutputFolder(It.IsAny<List<ICoverageProject>>())).
                Callback<List<ICoverageProject>>(coverageProjects =>
                {
                    coverageProjects[0].CoverageOutputFolder = "Set by ICoverageToolOutputManager";
                });

            ICoverageProject coverageProjectAfterCoverageOutputManager = null;
            var coverageUtilManager = mocker.GetMock<ICoverageUtilManager>();
            coverageUtilManager.Setup(mgr => mgr.RunCoverageAsync(It.IsAny<ICoverageProject>(), It.IsAny<CancellationToken>()))
                .Callback<ICoverageProject, CancellationToken>((cp, _) =>
                 {
                     coverageProjectAfterCoverageOutputManager = cp;
                 });

            await ReloadSuitableCoverageProject(mockCoverageProject => {
                mockCoverageProject.SetupProperty(cp => cp.CoverageOutputFolder);
                mockCoverageProject.Setup(p => p.StepAsync("Run Coverage Tool", It.IsAny<Func<ICoverageProject, Task>>())).Callback<string, Func<ICoverageProject, Task>>((_, runCoverTool) =>
                {
                    runCoverTool(mockCoverageProject.Object);
                });
            });

            Assert.AreEqual(coverageProjectAfterCoverageOutputManager.CoverageOutputFolder, "Set by ICoverageToolOutputManager");
        }

        [Test]
        public async Task Should_Run_Report_Generator_With_Output_Files_From_Coverage_For_Coverage_Projects_That_Have_Not_Failed()
        {
            var failedProject = CreateSuitableProject();
            failedProject.Setup(p => p.HasFailed).Returns(true);
            
            var passedProjectCoverageOutputFile = "outputfile.xml";
            var passedProject = CreateSuitableProject();
            passedProject.Setup(p => p.CoverageOutputFile).Returns(passedProjectCoverageOutputFile);
            
            mocker.GetMock<IReportGeneratorUtil>().Setup(rg => 
                rg.GenerateAsync(
                    It.Is<string[]>(
                        coverOutputFiles => coverOutputFiles.Count() == 1 && coverOutputFiles.First() == passedProjectCoverageOutputFile),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                    ).Result).Returns(new ReportGeneratorResult { });

            await ReloadInitializedCoverage(failedProject.Object, passedProject.Object);

            mocker.GetMock<IReportGeneratorUtil>().VerifyAll();
            
        }

        [Test]
        public async Task Should_Not_Run_ReportGenerator_If_No_Successful_Projects()
        {
            await ReloadInitializedCoverage();
            mocker.Verify<IReportGeneratorUtil>(rg => rg.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task Should_Process_ReportGenerator_Output_If_Success_Raising_Events()
        {
            var passedProject = CreateSuitableProject();
            var reportGeneratorResult = new ReportGeneratorResult
            {
                UnifiedXmlFile = "Unified xml file",
                UnifiedHtml = "Unified html",
                HotspotsFile = "Hotspots file path",

            };
            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.GenerateAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                    ).Result)
                .Returns(reportGeneratorResult);

            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml("Unified html", It.IsAny<string>())).Returns("processed");

            var mockCoberturaUtil = mocker.GetMock<ICoberturaUtil>();
            var fileLineCoverage = new FileLineCoverage();
            mockCoberturaUtil.Setup(coberturaUtil => coberturaUtil.ProcessCoberturaXml("Unified xml file")).Returns(fileLineCoverage);

            await ReloadInitializedCoverage(passedProject.Object);
            
            mockReportGenerator.VerifyAll();
            mockReportGenerator.Verify(reportGenerator => reportGenerator.EndOfCoverageRun());

            mocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(
                It.Is<NewCoverageLinesMessage>(message => message.CoverageLines == fileLineCoverage),
                null
                ));

            mocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(
                It.Is<NewReportMessage>(message => message.Report == "processed"),
                null
                ));

            mocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(
                It.Is<ReportFilesMessage>(message =>  message.CoberturaFile == reportGeneratorResult.UnifiedXmlFile &&  message.HotspotsFile == reportGeneratorResult.HotspotsFile),
                null
                ));

        }

        [Test]
        public async Task Should_Cancel_Running_Coverage_Logging_Cancelled_When_StopCoverage()
        {
            await StopCoverage();
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
        }

        [Test]
        public void Should_Not_Throw_When_StopCoverage_And_There_Is_No_Coverage_Running()
        {
            fccEngine.StopCoverage();
        } 

        [Test]
        public async Task Should_Cancel_Existing_ReloadCoverage_When_ReloadCoverage()
        {
            SetUpSuccessfulRunReportGenerator();

            var mockSuitableCoverageProject = new Mock<ICoverageProject>();
            mockSuitableCoverageProject.Setup(p => p.ProjectFile).Returns("Defined.csproj");
            mockSuitableCoverageProject.Setup(p => p.Settings.Enabled).Returns(true);

            Task t = new Task(() =>
            {

            });
            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync(It.IsAny<CancellationToken>(), true)).Callback(() =>
            {
                fccEngine.ReloadCoverage(()=>Task.FromResult(new List<ICoverageProject>()));
                Thread.Sleep(1000);
                t.Start();

            }).Returns(Task.FromResult(new CoverageProjectFileSynchronizationDetails()));

            await ReloadInitializedCoverage(mockSuitableCoverageProject.Object);
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Cancelled);


        }

        private void VerifyLogsReloadCoverageStatus(ReloadCoverageStatus reloadCoverageStatus)
        {
            mocker.Verify<ILogger>(l => l.Log(fccEngine.GetLogReloadCoverageStatusMessage(reloadCoverageStatus)));
        }

        private async Task<(string reportGeneratedHtmlContent, FileLineCoverage updatedCoverageLines)> RunToCompletion(bool noCoverageProjects)
        {
            var coverageProject = CreateSuitableProject().Object;
            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.GenerateAsync(
                    It.Is<IEnumerable<string>>(coverOutputFiles => coverOutputFiles.Count() == 1 && coverOutputFiles.First() == coverageProject.CoverageOutputFile),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        UnifiedHtml = "Unified"
                    }
                );

            var reportGeneratedHtmlContent = "<somehtml/>";
            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml("Unified", It.IsAny<string>())).Returns(reportGeneratedHtmlContent);
            var coverageLines = new FileLineCoverage();
            coverageLines.Add("test", new[] { new Line() });
            coverageLines.Completed();
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.ProcessCoberturaXml(It.IsAny<string>())).Returns(coverageLines);
            if (noCoverageProjects)
            {
                await ReloadInitializedCoverage();
            }
            else
            {
                await ReloadInitializedCoverage(coverageProject);
            }

            return (reportGeneratedHtmlContent, coverageLines);

        }

        private async Task ThrowReadingReportHtml()
        {
            var passedProject = CreateSuitableProject();

            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.GenerateAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                 ).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                    }
                );

            var coverageLines = new FileLineCoverage();
            coverageLines.Add("test", new[] { new Line() });
            coverageLines.Completed();
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.ProcessCoberturaXml(It.IsAny<string>())).Returns(coverageLines);

            await ReloadInitializedCoverage(passedProject.Object);

        }

        private async Task StopCoverage()
        {
            var mockSuitableCoverageProject = new Mock<ICoverageProject>();
            mockSuitableCoverageProject.Setup(p => p.ProjectFile).Returns("Defined.csproj");
            mockSuitableCoverageProject.Setup(p => p.Settings.Enabled).Returns(true);

            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync(It.IsAny<CancellationToken>(), true)).Callback(() =>
            {
                fccEngine.StopCoverage();

            }).Returns(Task.FromResult(new CoverageProjectFileSynchronizationDetails()));

            await ReloadInitializedCoverage(mockSuitableCoverageProject.Object);
        }

        private void SetUpSuccessfulRunReportGenerator()
        {
            mocker.GetMock<IReportGeneratorUtil>()
                .Setup(rg => rg.GenerateAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                    ).Result)
                .Returns(new ReportGeneratorResult {  });
        }

        private async Task ReloadInitializedCoverage(params ICoverageProject[] coverageProjects)
        {
            var projectsFromTask = Task.FromResult(coverageProjects.ToList());
            fccEngine.Initialize(CancellationToken.None);
            fccEngine.ReloadCoverage(() => projectsFromTask);
            await fccEngine.reloadCoverageTask;
        }
        private Mock<ICoverageProject> CreateSuitableProject()
        {
            var mockSuitableCoverageProject = new Mock<ICoverageProject>();
            mockSuitableCoverageProject.Setup(p => p.ProjectFile).Returns("Defined.csproj");
            mockSuitableCoverageProject.Setup(p => p.Settings.Enabled).Returns(true);
            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync(It.IsAny<CancellationToken>(), true)).Returns(Task.FromResult(new CoverageProjectFileSynchronizationDetails()));
            mockSuitableCoverageProject.Setup(p => p.StepAsync("Run Coverage Tool", It.IsAny<Func<ICoverageProject, Task>>())).Returns(Task.CompletedTask);
            return mockSuitableCoverageProject;
        }
        private async Task<Mock<ICoverageProject>> ReloadSuitableCoverageProject(Action<Mock<ICoverageProject>> setUp = null)
        {
            var mockSuitableCoverageProject = CreateSuitableProject();
            setUp?.Invoke(mockSuitableCoverageProject);
            SetUpSuccessfulRunReportGenerator();
            await ReloadInitializedCoverage(mockSuitableCoverageProject.Object);
            return mockSuitableCoverageProject;
        }

    }

}