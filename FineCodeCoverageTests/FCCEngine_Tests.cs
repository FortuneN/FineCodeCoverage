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
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
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
        private List<CoverageLine> updatedCoverageLines;
        private string htmlContent;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();
            fccEngine.UpdateMarginTags += (UpdateMarginTagsEventArgs e) =>
            {
                updatedCoverageLines = e.CoverageLines;
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

            var coverletMock = mocker.GetMock<ICoverletUtil>().Setup(coverlet => coverlet.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(2));

            var reportGeneratorMock = mocker.GetMock<IReportGeneratorUtil>().Setup(reportGenerator => reportGenerator.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(3));

            var msTestPlatformMock = mocker.GetMock<IMsTestPlatformUtil>().Setup(msTestPlatform => msTestPlatform.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(4));

            var openCoverMock = mocker.GetMock<IOpenCoverUtil>().Setup(openCover => openCover.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(5));

            fccEngine.Initialize(null);

            Assert.AreEqual(5, callOrder.Count);
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
        public void Should_UpdateMarginTags_With_No_CoverageLines()
        {
            fccEngine.ClearUI();
            Assert.AreEqual(0, updatedCoverageLines.Count);
        }
    }

    public class FCCEngine_ReloadCoverage_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;
        private bool raisedUpdateMarginTags;
        private bool raisedUpdateOutputWindow;
        private string tempReportGeneratedHtml;
        private List<CoverageLine> updatedCoverageLines;
        private string htmlContent;

        private void VerifyLogsReloadCoverageStatus(ReloadCoverageStatus reloadCoverageStatus)
        {
            mocker.Verify<ILogger>(l => l.Log(fccEngine.GetLogReloadCoverageStatusMessage(reloadCoverageStatus)));
        }
        private void VerifyRaisedUIEvents()
        {
            Assert.True(raisedUpdateOutputWindow);
            Assert.True(raisedUpdateMarginTags);
        }
        private void VerifyClearUIEvents()
        {
            VerifyRaisedUIEvents();
            Assert.IsNull(updatedCoverageLines);
            Assert.IsNull(htmlContent);
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

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();
            
            raisedUpdateMarginTags = false;
            raisedUpdateOutputWindow = false;
            tempReportGeneratedHtml = null;
            updatedCoverageLines = null;
            htmlContent = null;

            fccEngine.UpdateMarginTags += (UpdateMarginTagsEventArgs e) =>
            {
                raisedUpdateMarginTags = true;
                updatedCoverageLines = e.CoverageLines;
            };
            
            fccEngine.UpdateOutputWindow += (UpdateOutputWindowEventArgs e) =>
            {
                raisedUpdateOutputWindow = true;
                htmlContent = e.HtmlContent;
            };
        }

        [TearDown]
        public void Delete_ReportGeneratedHtml()
        {
            if (tempReportGeneratedHtml != null)
            {
                File.Delete(tempReportGeneratedHtml);
            }
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

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Run_The_Appropriate_Cover_Tool_Based_On_IsDotNetSdkStyle(bool isDotNetSdkStyle)
        {
            Task waitForCoverage = null;
            ICoverageProject coverageProject = null;
            await ReloadSuitableCoverageProject(mockCoverageProject => {
                coverageProject = mockCoverageProject.Object;
                mockCoverageProject.Setup(p => p.IsDotNetSdkStyle()).Returns(isDotNetSdkStyle);
                mockCoverageProject.Setup(p => p.StepAsync("Run Coverage Tool", It.IsAny<Func<ICoverageProject, Task>>())).Callback<string,Func<ICoverageProject, Task>>((_,runCoverTool) =>
                {
                    waitForCoverage = runCoverTool(coverageProject);
                });
            });
            if (isDotNetSdkStyle)
            {
                mocker.Verify<ICoverletUtil>(coverlet => coverlet.RunCoverletAsync(coverageProject, true));
            }
            else
            {
                mocker.Verify<IOpenCoverUtil>(openCover => openCover.RunOpenCoverAsync(coverageProject, true));
            }
            
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

            var unifiedXmlFile = "unified.xml";
            var unifiedHtmlFile = "unified.html";
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
                        UnifiedXmlFile = unifiedXmlFile,
                        UnifiedHtmlFile = unifiedHtmlFile
                    }
                );

            var _ = "";
            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtmlFile(unifiedHtmlFile, It.IsAny<bool>(), out _));

            await ReloadInitializedCoverage(passedProject.Object);
            mocker.Verify<ICoberturaUtil>(coberturaUtil => coberturaUtil.ProcessCoberturaXmlFile(unifiedXmlFile));
            mockReportGenerator.VerifyAll();
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
            mocker.Verify<ICoberturaUtil>(coberturaUtil => coberturaUtil.ProcessCoberturaXmlFile(It.IsAny<string>()), Times.Never());
            
        }

        private async Task<(string reportGeneratedHtmlContent,List<CoverageLine> updatedCoverageLines)> RunToCompletion(bool early)
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

            var reportGeneratedHtmlContent = "<somehtml/>";
            tempReportGeneratedHtml = Path.GetTempFileName();
            File.WriteAllText(tempReportGeneratedHtml, reportGeneratedHtmlContent);
            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtmlFile(It.IsAny<string>(), It.IsAny<bool>(), out tempReportGeneratedHtml));

            List<CoverageLine> coverageLines = new List<CoverageLine>() { new CoverageLine() };
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.CoverageLines).Returns(coverageLines);
            if (early)
            {
                await ReloadInitializedCoverage();
            }
            else
            {
                await ReloadInitializedCoverage(passedProject.Object);
            }
            
            return (reportGeneratedHtmlContent, coverageLines);
            
        }

        [Test]
        public async Task Should_Update_UI_When_ReloadCoverage_Completes_Fully()
        {
            var mocked = await RunToCompletion(false);
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Done);
            Assert.AreSame(mocked.updatedCoverageLines, updatedCoverageLines);
            Assert.AreEqual(mocked.reportGeneratedHtmlContent, htmlContent);

        }

        [Test]
        public async Task Should_Clear_UI_When_ReloadCoverage_Completes_Early_With_Reset()
        {
            await RunToCompletion(true);
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Done);
            VerifyClearUIEvents();
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
            mockReportGenerator.Setup(rg => rg.ProcessUnifiedHtmlFile(It.IsAny<string>(), It.IsAny<bool>(), out badPath));

            List<CoverageLine> coverageLines = new List<CoverageLine>() { new CoverageLine() };
            mocker.GetMock<ICoberturaUtil>().Setup(coberturaUtil => coberturaUtil.CoverageLines).Returns(coverageLines);
            
            await ReloadInitializedCoverage(passedProject.Object);

        }

        [Test] // CoverageLines will be present and tags added hence done - unlikely for this branch to occur
        public async Task Should_Log_Done_When_Exception_Reading_Report_Html()
        {
            await ThrowReadingReportHtml();
            VerifyLogsReloadCoverageStatus(ReloadCoverageStatus.Done);
            mocker.Verify<ILogger>(l => l.Log(FCCEngine.errorReadingReportGeneratorOutputMessage));
        }

        [Test]
        public async Task Should_Log_Message_When_Exception_Reading_Report_Html()
        {
            await ThrowReadingReportHtml();
            mocker.Verify<ILogger>(l => l.Log(FCCEngine.errorReadingReportGeneratorOutputMessage));
        }

        [Test]
        public async Task Should_Update_OutputWindow_With_Null_HtmlContent_When_Reading_Report_Html_Throws()
        {
            await ThrowReadingReportHtml();
            Assert.True(raisedUpdateOutputWindow);
            Assert.IsNull(htmlContent);

        }

        private async Task ThrowException(Action<Exception> exceptionCallback = null)
        {
            var exception = new Exception("an exception");
            exceptionCallback?.Invoke(exception);
            Task<List<ICoverageProject>> thrower() => Task.FromException<List<ICoverageProject>>(exception);

            var mockInitializeStatusProvider  = new Mock<IInitializeStatusProvider>();
            mockInitializeStatusProvider.Setup(i => i.InitializeStatus).Returns(InitializeStatus.Initialized);
            fccEngine.Initialize(mockInitializeStatusProvider.Object);

            fccEngine.ReloadCoverage(thrower);
            
            await fccEngine.reloadCoverageTask;
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
            await ThrowException();
            VerifyClearUIEvents();
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
            Assert.False(raisedUpdateMarginTags);
            Assert.False(raisedUpdateOutputWindow);

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
        
    }

}