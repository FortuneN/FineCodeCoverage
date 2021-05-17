using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Impl;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class FCCEngine_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;
        private string htmlContent;
        private bool updatedMarginTags;

        [SetUp]
        public void SetUp()
        {
            updatedMarginTags = false;
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();
            fccEngine.UpdateMarginTags += (UpdateMarginTagsEventArgs e) =>
            {
                updatedMarginTags = true;
            };


            fccEngine.UpdateOutputWindow += (UpdateOutputWindowEventArgs e) =>
            {
                htmlContent = e.HtmlContent;
            };
        }

        [Test]
        public void Should_Initialize_AppFolder_Then_Utils()
        {
            List<int> callOrder = new List<int>();

            var appDataFolderPath = "some path";
            var mockAppDataFolder = mocker.GetMock<IAppDataFolder>();
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.Initialize()).Callback(() => callOrder.Add(1));
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.DirectoryPath).Returns(appDataFolderPath);

            var reportGeneratorMock = mocker.GetMock<IReportGeneratorUtil>().Setup(reportGenerator => reportGenerator.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(2));

            var msTestPlatformMock = mocker.GetMock<IMsTestPlatformUtil>().Setup(msTestPlatform => msTestPlatform.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(3));

            var openCoverMock = mocker.GetMock<ICoverageUtilManager>().Setup(openCover => openCover.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(4));

            fccEngine.Initialize(null);

            Assert.AreEqual(4, callOrder.Count);
            Assert.AreEqual(1, callOrder[0]);
        }

        
        [Test]
        public void Should_Set_AppDataFolderPath_From_Initialized_AppDataFolder_DirectoryPath()
        {
            var appDataFolderPath = "some path";
            var mockAppDataFolder = mocker.GetMock<IAppDataFolder>();
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.DirectoryPath).Returns(appDataFolderPath);
            fccEngine.Initialize(null);
            Assert.AreEqual("some path", fccEngine.AppDataFolderPath);
        }
    
        [Test]
        public void Should_Update_The_Output_Window_With_Null_HtmlContent_When_ClearUI()
        {
            fccEngine.ClearUI();
            Assert.Null(htmlContent);
        }

        [Test]
        public void Should_UpdateMarginTags_And_Set_Null_CoverageLines_When_ClearUI()
        {
            fccEngine.CoverageLines = new List<CoverageLine>();
            fccEngine.ClearUI();
            Assert.IsTrue(updatedMarginTags);
            Assert.IsNull(fccEngine.CoverageLines);
        }

        [Test]
        public void Should_Begin_With_Null_CoverageLines()
        {
            Assert.IsNull(fccEngine.CoverageLines);
        }
    }

    public class FCCEngine_ReloadCoverage_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;
        private List<UpdateMarginTagsEventArgs> updateMarginTagsEvents;
        private List<List<CoverageLine>> updateMarginTagsCoverageLines;
        private List<UpdateOutputWindowEventArgs> updateOutputWindowEvents;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();

            updateMarginTagsEvents = new List<UpdateMarginTagsEventArgs>();
            updateMarginTagsCoverageLines = new List<List<CoverageLine>>();
            updateOutputWindowEvents = new List<UpdateOutputWindowEventArgs>();

            fccEngine.UpdateMarginTags += (UpdateMarginTagsEventArgs e) =>
            {
                updateMarginTagsEvents.Add(e);
                updateMarginTagsCoverageLines.Add(fccEngine.CoverageLines);
            };
            
            fccEngine.UpdateOutputWindow += (UpdateOutputWindowEventArgs e) =>
            {
                updateOutputWindowEvents.Add(e);
            };
        }

        [Test]
        public async Task Should_Log_Starting_When_Initialized()
        {
            await ReloadInitializedCoverage();
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Start);
        }

        [Test]
        public async Task Should_Poll_For_Initialized()
        {
            var times = 5;
            var initializeWait = 1000;
            fccEngine.InitializeWait = initializeWait;

            var mockInitializeStatusProvider = new Mock<IInitializeStatusProvider>();
            mockInitializeStatusProvider.SetupProperty(i => i.InitializeStatus);
            var initializeStatusProvider = mockInitializeStatusProvider.Object;

            fccEngine.Initialize(initializeStatusProvider);

            fccEngine.ReloadCoverage(() => Task.FromResult(new List<ICoverageProject>()));
            await Task.Delay(times * initializeWait).ContinueWith(_ =>
            {
                initializeStatusProvider.InitializeStatus = InitializeStatus.Initialized;
            });
            await fccEngine.reloadCoverageTask;
            mocker.Verify<ILogger>(l => l.Log(fccEngine.GetLogReloadCoverageStatusMessage(ReloadCoverageStatus.Initializing)), Times.AtLeast(times));
        }

        [Test]
        public async Task Should_Throw_With_initializationFailedMessagePrefix_When_Initialize_Has_Failed()
        {
            var mockInitializerStatusProvider = new Mock<IInitializeStatusProvider>();
            mockInitializerStatusProvider.Setup(i => i.InitializeStatus).Returns(InitializeStatus.Error);
            var initializeExceptionMessage = "An exception was thrown";
            mockInitializerStatusProvider.Setup(i => i.InitializeExceptionMessage).Returns(initializeExceptionMessage);
            fccEngine.Initialize(mockInitializerStatusProvider.Object);

            fccEngine.ReloadCoverage(() => Task.FromResult(new List<ICoverageProject>()));
            
            await fccEngine.reloadCoverageTask;
            mocker.Verify<ILogger>(l => l.Log(It.Is<Exception>(exc => (FCCEngine.initializationFailedMessagePrefix + Environment.NewLine + initializeExceptionMessage) == exc.Message)));
            
        }

        [Test]
        public async Task Should_Enlist_The_ProcessUtil_In_The_Same_Cancellable_Run()
        {
            await ReloadInitializedCoverage();
            var mockProcessUtil = mocker.GetMock<IProcessUtil>();
            mockProcessUtil.VerifySet(p => p.CancellationToken = It.IsAny<CancellationToken>());
        }

        [Test]
        public async Task Should_Prepare_For_Coverage_Suitable_CoverageProjects()
        {
            var mockSuitableCoverageProject = await ReloadSuitableCoverageProject();
            mockSuitableCoverageProject.Verify(p => p.PrepareForCoverageAsync());
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

            mocker.Verify<ICoverageUtilManager>(coverageUtilManager => coverageUtilManager.RunCoverageAsync(coverageProject, true));

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
            coverageUtilManager.Setup(mgr => mgr.RunCoverageAsync(It.IsAny<ICoverageProject>(), It.IsAny<bool>()))
                .Callback<ICoverageProject, bool>((cp, _) =>
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

        [Test] // Not testing dark mode as ui will change
        public async Task Should_Run_Report_Generator_With_Output_Files_From_Coverage_For_Coverage_Projects_That_Have_Not_Failed()
        {
            var failedProject = CreateSuitableProject();
            failedProject.Setup(p => p.HasFailed).Returns(true);
            
            var passedProjectCoverageOutputFile = "outputfile.xml";
            var passedProject = CreateSuitableProject();
            passedProject.Setup(p => p.CoverageOutputFile).Returns(passedProjectCoverageOutputFile);
            
            mocker.GetMock<IReportGeneratorUtil>().Setup(rg => 
                rg.RunReportGeneratorAsync(
                    It.Is<string[]>(
                        coverOutputFiles => coverOutputFiles.Count() == 1 && coverOutputFiles.First() == passedProjectCoverageOutputFile),
                    It.IsAny<bool>(),
                    true).Result).Returns(new ReportGeneratorResult { Success = true });

            await ReloadInitializedCoverage(failedProject.Object, passedProject.Object);

            mocker.GetMock<IReportGeneratorUtil>().VerifyAll();
            
        }

        [Test]
        public async Task Should_Not_Run_ReportGenerator_If_No_Successful_Projects()
        {
            await ReloadInitializedCoverage();
            mocker.Verify<IReportGeneratorUtil>(rg => rg.RunReportGeneratorAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public async Task Should_Process_ReportGenerator_Output_If_Success()
        {
            var passedProject = CreateSuitableProject();

            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = true,
                        UnifiedXml = "Unified xml",
                        UnifiedHtml = "Unified html"
                    }
                );

            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml("Unified html", It.IsAny<bool>()));

            await ReloadInitializedCoverage(passedProject.Object);
            mocker.Verify<ICoberturaUtil>(coberturaUtil => coberturaUtil.ProcessCoberturaXml("Unified xml"));
            mockReportGenerator.VerifyAll();
        }

        [Test]
        public async Task Should_Set_Report_Output_If_Success()
        {
            var passedProject = CreateSuitableProject();

            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = true,
                        UnifiedXml = "Unified xml",
                        UnifiedHtml = "Unified html"
                    }
                );

            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml("Unified html", It.IsAny<bool>())).Returns("Processed html");

            await ReloadInitializedCoverage(passedProject.Object);
            mocker.Verify<ICoverageToolOutputManager>(coverageToolOutputManager => coverageToolOutputManager.OutputReports("Unified html","Processed html","Unified xml"));
        }

        [Test]
        public async Task Should_Not_Process_ReportGenerator_Output_If_Failure()
        {
            var passedProject = CreateSuitableProject();

            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = false
                    }
                );

            await ReloadInitializedCoverage(passedProject.Object);
            mocker.Verify<ICoberturaUtil>(coberturaUtil => coberturaUtil.ProcessCoberturaXml(It.IsAny<string>()), Times.Never());
            
        }

        [Test]
        public async Task Should_Not_Set_Report_Output_If_Failure()
        {
            var passedProject = CreateSuitableProject();

            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = false,
                    }
                );

            await ReloadInitializedCoverage(passedProject.Object);
            mocker.Verify<ICoverageToolOutputManager>(coverageToolOutputManager => coverageToolOutputManager.OutputReports(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),Times.Never());
        }

        [Test]
        public async Task Should_Clear_UI_Then_Update_UI_When_ReloadCoverage_Completes_Fully()
        {
            fccEngine.CoverageLines = new List<CoverageLine>();
            var (reportGeneratedHtmlContent, updatedCoverageLines) = await RunToCompletion(false);

            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Done);

            VerifyClearUIEvents(0);

            Assert.AreSame(updatedCoverageLines, updateMarginTagsCoverageLines[1]);
            Assert.AreEqual(reportGeneratedHtmlContent, updateOutputWindowEvents[1].HtmlContent);

        }

        [Test]
        public async Task Should_Clear_UI_When_ReloadCoverage_And_No_CoverageProjects()
        {
            fccEngine.CoverageLines = new List<CoverageLine>();

            await RunToCompletion(true);
            
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Done);
            
            Assert.Null(updateMarginTagsCoverageLines[1]);
            Assert.Null(updateOutputWindowEvents[1].HtmlContent);
        }
        
        [Test]
        public async Task Should_Update_OutputWindow_With_Null_HtmlContent_When_Reading_Report_Html_Throws()
        {
            await ThrowReadingReportHtml();
            
            Assert.AreEqual(updateMarginTagsCoverageLines[1].Count, 1);
            Assert.Null(updateOutputWindowEvents[1].HtmlContent);

        }
        
        [Test]
        public async Task Should_Log_Single_Exception_From_Aggregate_Exception()
        {
            Exception exception = null;
            await ThrowException(exc => exception = exc);
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Error);
            mocker.Verify<ILogger>(l => l.Log(exception));
        }

        [Test]
        public async Task Should_Clear_UI_When_There_Is_An_Exception()
        {
            fccEngine.CoverageLines = new List<CoverageLine>();
            await ThrowException();
            VerifyClearUIEvents(1);
        }

        [Test]
        public async Task Should_Cancel_Running_Coverage_Logging_Cancelled_When_StopCoverage()
        {
            await StopCoverage();
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Cancelled);
        }

        [Test]
        public async Task Should_Not_Update_UI_When_ReloadCoverage_Is_Cancelled()
        {
            await StopCoverage();
            Assert.AreEqual(1, updateMarginTagsEvents.Count);
            Assert.AreEqual(1, updateOutputWindowEvents.Count);

        }

        [Test]
        public async Task Should_Cancel_ProcessUtil_Tasks_When_StopCoverage()
        {
            Task processUtilTask = null;
            mocker.GetMock<IProcessUtil>().SetupSet(p => p.CancellationToken = It.IsAny<CancellationToken>()).Callback<CancellationToken>(ct =>
              {
                  processUtilTask = Task.Delay(1000000000, ct);
              });
            await StopCoverage();
            Assert.AreEqual(TaskStatus.Canceled, processUtilTask.Status);
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
            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync()).Callback(() =>
            {
                fccEngine.ReloadCoverage(()=>Task.FromResult(new List<ICoverageProject>()));
                Thread.Sleep(1000);
                t.Start();

            }).Returns(Task.CompletedTask);

            await ReloadInitializedCoverage(mockSuitableCoverageProject.Object);
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Cancelled);


        }
        
        private void VerifyLogsReloadCoverageStatus(ReloadCoverageStatus reloadCoverageStatus)
        {
            mocker.Verify<ILogger>(l => l.Log(fccEngine.GetLogReloadCoverageStatusMessage(reloadCoverageStatus)));
        }

        private void VerifyClearUIEvents(int eventNumber)
        {
            Assert.Null(updateMarginTagsCoverageLines[eventNumber]);
            Assert.Null(updateOutputWindowEvents[eventNumber].HtmlContent);
        }

        private async Task<(string reportGeneratedHtmlContent, List<CoverageLine> updatedCoverageLines)> RunToCompletion(bool noCoverageProjects)
        {
            var coverageProject = CreateSuitableProject().Object;
            var mockReportGenerator = mocker.GetMock<IReportGeneratorUtil>();
            mockReportGenerator.Setup(rg =>
                rg.RunReportGeneratorAsync(
                    It.Is<IEnumerable<string>>(coverOutputFiles => coverOutputFiles.Count() == 1 && coverOutputFiles.First() == coverageProject.CoverageOutputFile),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = true,
                        UnifiedHtml = "Unified"
                    }
                );

            var reportGeneratedHtmlContent = "<somehtml/>";
            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml("Unified", It.IsAny<bool>())).Returns(reportGeneratedHtmlContent);

            List<CoverageLine> coverageLines = new List<CoverageLine>() { new CoverageLine() };
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.CoverageLines).Returns(coverageLines);
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
                rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    true).Result)
                .Returns(
                    new ReportGeneratorResult
                    {
                        Success = true,
                    }
                );

            var badPath = "^&$!";
            //mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtml(It.IsAny<string>(), It.IsAny<bool>(), out badPath));

            List<CoverageLine> coverageLines = new List<CoverageLine>() { new CoverageLine() };
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.CoverageLines).Returns(coverageLines);

            await ReloadInitializedCoverage(passedProject.Object);

        }

        private async Task ThrowException(Action<Exception> exceptionCallback = null)
        {
            var exception = new Exception("an exception");
            exceptionCallback?.Invoke(exception);
            Task<List<ICoverageProject>> thrower() => Task.FromException<List<ICoverageProject>>(exception);

            var mockInitializeStatusProvider = new Mock<IInitializeStatusProvider>();
            mockInitializeStatusProvider.Setup(i => i.InitializeStatus).Returns(InitializeStatus.Initialized);
            fccEngine.Initialize(mockInitializeStatusProvider.Object);

            fccEngine.ReloadCoverage(thrower);

            await fccEngine.reloadCoverageTask;
        }

        private async Task StopCoverage()
        {
            var mockSuitableCoverageProject = new Mock<ICoverageProject>();
            mockSuitableCoverageProject.Setup(p => p.ProjectFile).Returns("Defined.csproj");
            mockSuitableCoverageProject.Setup(p => p.Settings.Enabled).Returns(true);

            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync()).Callback(() =>
            {
                fccEngine.StopCoverage();

            }).Returns(Task.CompletedTask);

            await ReloadInitializedCoverage(mockSuitableCoverageProject.Object);
        }

        private void SetUpSuccessfulRunReportGenerator()
        {
            mocker.GetMock<IReportGeneratorUtil>()
                .Setup(rg => rg.RunReportGeneratorAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                    ).Result)
                .Returns(new ReportGeneratorResult { Success = true });
        }

        private async Task ReloadInitializedCoverage(params ICoverageProject[] coverageProjects)
        {
            var projectsFromTask = Task.FromResult(coverageProjects.ToList());
            var mockInitializeStatusProvider = new Mock<IInitializeStatusProvider>();
            mockInitializeStatusProvider.Setup(i => i.InitializeStatus).Returns(InitializeStatus.Initialized);
            fccEngine.Initialize(mockInitializeStatusProvider.Object);
            fccEngine.ReloadCoverage(() => projectsFromTask);
            await fccEngine.reloadCoverageTask;
        }
        private Mock<ICoverageProject> CreateSuitableProject()
        {
            var mockSuitableCoverageProject = new Mock<ICoverageProject>();
            mockSuitableCoverageProject.Setup(p => p.ProjectFile).Returns("Defined.csproj");
            mockSuitableCoverageProject.Setup(p => p.Settings.Enabled).Returns(true);
            mockSuitableCoverageProject.Setup(p => p.PrepareForCoverageAsync()).Returns(Task.CompletedTask);
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