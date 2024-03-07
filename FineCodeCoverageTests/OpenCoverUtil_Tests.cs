using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverageTests
{
    internal class OpenCoverUtil_Tests
    {
        private AutoMoqer mocker;
        private OpenCoverUtil openCoverUtil;
        private Mock<IFileUtil> mockFileUtil;
        private const string openCoverExePath = "OpenCover.Console.exe";

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            openCoverUtil = mocker.Create<OpenCoverUtil>();
        }

        [Test]
        public void Should_Ensure_Unzipped_For_The_OpenCover_Exe_Path_When_Initialize()
        {
            Initialize();

            mockFileUtil.VerifyAll();
        }

        private void Initialize()
        {
            var ct = CancellationToken.None;

            mocker.Setup<IToolUnzipper, string>(toolUnzipper => toolUnzipper.EnsureUnzipped("appDataFolder", "openCover", "openCover", ct)).Returns("toolFolderPath");
            mockFileUtil = mocker.GetMock<IFileUtil>();

            mockFileUtil.Setup(fileUtil => fileUtil.GetFiles("toolFolderPath", "OpenCover.Console.exe", System.IO.SearchOption.AllDirectories))
                .Returns(new string[] {openCoverExePath });

            openCoverUtil.Initialize("appDataFolder", ct);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Delete_The_Test_Pdb_When_RunOpenCoverAsync_And_IncludeTestAssembly_Is_False_Async(bool includeTestAssembly)
        {
            var ct = CancellationToken.None;
            mocker.Setup<IOpenCoverExeArgumentsProvider,List<string>>(openCoverExeArgumentsProvider => openCoverExeArgumentsProvider.Provide(
                It.IsAny<ICoverageProject>(),It.IsAny<string>())).Returns(new List<string>());
                
            var mockProcessUtil = mocker.GetMock<IProcessUtil>();
            mockProcessUtil.Setup(processUtil => processUtil.ExecuteAsync(It.IsAny<ExecuteRequest>(),ct)).ReturnsAsync(new ExecuteResponse());

            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(includeTestAssembly);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ProjectOutputFolder).Returns("projectOutputFolder");
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("ATestDll.dll");

            await openCoverUtil.RunOpenCoverAsync(mockCoverageProject.Object, ct);

            var pdbFilePath = Path.Combine("projectOutputFolder", "ATestDll.pdb");
            mocker.Verify<IFileUtil>(fileUtil => fileUtil.DeleteFile(pdbFilePath), includeTestAssembly ? Times.Never() : Times.Once());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Execute_OpenCover_With_The_Provided_Arguments_When_RunOpenCoverAsync(bool useCustomExe)
        {
            var ct = CancellationToken.None;
            mocker.Setup<IMsTestPlatformUtil, string>(msTestPlatformUtil => msTestPlatformUtil.MsTestPlatformExePath).Returns("MsTestPlatformExePath");
            var mockProcessUtil = mocker.GetMock<IProcessUtil>();
            mockProcessUtil.Setup(processUtil => processUtil.ExecuteAsync(It.IsAny<ExecuteRequest>(), ct)).ReturnsAsync(new ExecuteResponse());

            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(true);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.OpenCoverCustomPath).Returns(useCustomExe ? "CustomOpenCoverExePath" : null);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ProjectOutputFolder).Returns("ProjectOutputFolder");

            var arguments = new List<string> { "First","Second"};
            var mockOpenCoverExeArgumentsProvider = mocker.GetMock<IOpenCoverExeArgumentsProvider>();
            mockOpenCoverExeArgumentsProvider.Setup(openCoverExeArgumentsProvider => openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "MsTestPlatformExePath"))
                .Returns(arguments);

            Initialize();
            await openCoverUtil.RunOpenCoverAsync(mockCoverageProject.Object, ct);

            var expectedArguments = string.Join(" ", arguments);
            var expectedExePath = useCustomExe ? "CustomOpenCoverExePath" : openCoverExePath;
            mockProcessUtil.Verify(processUtil => processUtil.ExecuteAsync(
                It.Is<ExecuteRequest>(
                    executeRequest => executeRequest.FilePath == expectedExePath && executeRequest.Arguments == expectedArguments && executeRequest.WorkingDirectory == "ProjectOutputFolder"), ct), 
                    Times.Once()
            );
        }

        [Test]
        public void Should_Throw_Exception_With_The_Result_Output_When_ExitCode_Is_Not_0()
        {
            var ct = CancellationToken.None;
            mocker.Setup<IOpenCoverExeArgumentsProvider, List<string>>(openCoverExeArgumentsProvider => openCoverExeArgumentsProvider.Provide(
                It.IsAny<ICoverageProject>(), It.IsAny<string>())).Returns(new List<string>());
            mocker.Setup<IMsTestPlatformUtil, string>(msTestPlatformUtil => msTestPlatformUtil.MsTestPlatformExePath).Returns("MsTestPlatformExePath");
            var mockProcessUtil = mocker.GetMock<IProcessUtil>();
            mockProcessUtil.Setup(processUtil => processUtil.ExecuteAsync(It.IsAny<ExecuteRequest>(), ct)).ReturnsAsync(new ExecuteResponse
            {
                ExitCode = 1,
                Output = "Output"
            });

            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(true);

            Assert.ThrowsAsync<Exception>(async () =>  await openCoverUtil.RunOpenCoverAsync(mockCoverageProject.Object, ct), "Output");
        }

        //todo logging tests
    }
}
