using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class CoverletDataCollectorUtil_RunAsync_Tests
    {
        private AutoMoqer mocker;
        private CoverletDataCollectorUtil coverletDataCollectorUtil;
        private Mock<ICoverageProject> mockCoverageProject;
        private Mock<IRunSettingsCoverletConfiguration> mockRunSettingsCoverletConfiguration;
        private Mock<IDataCollectorSettingsBuilder> mockDataCollectorSettingsBuilder;

        private string tempDirectory;
        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            mockDataCollectorSettingsBuilder = new Mock<IDataCollectorSettingsBuilder>();
            mocker.GetMock<IDataCollectorSettingsBuilderFactory>().Setup(f => f.Create()).Returns(mockDataCollectorSettingsBuilder.Object);

            coverletDataCollectorUtil = mocker.Create<CoverletDataCollectorUtil>();

            mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.Settings).Returns(new Mock<IAppOptions>().Object);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(new List<string>());
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(new List<string>());
            mockRunSettingsCoverletConfiguration = new Mock<IRunSettingsCoverletConfiguration>();
            coverletDataCollectorUtil.runSettingsCoverletConfiguration = mockRunSettingsCoverletConfiguration.Object;
            coverletDataCollectorUtil.coverageProject = mockCoverageProject.Object;
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            if(tempDirectory != null && Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory);
            }
        }

        private DirectoryInfo CreateTemporaryDirectory()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
        }


        [Test]
        public async Task Should_Get_Settings_With_TestDllFile()
        {
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns("test.dll");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");

            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithProjectDll("test.dll"));

        }

        [Test]
        public async Task Should_Get_Settings_With_Exclude_From_CoverageProject_And_RunSettings()
        {
            var projectExclude = new string[] { "excluded" };
            mockCoverageProject.Setup(cp => cp.Settings.Exclude).Returns(projectExclude);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            var referencedExcluded = new List<string> { "referencedExcluded" };
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(referencedExcluded);
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.Exclude).Returns("rsexclude");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithExclude(new string[] { "[referencedExcluded]*","excluded"},"rsexclude"));
        }

        [Test]
        public async Task Should_Not_Throw_When_Project_Setttings_Exclude_Is_Null()
        {
            var referencedExcluded = new List<string> { "referencedExcluded" };
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(referencedExcluded);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.Exclude).Returns("rsexclude");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithExclude(new string[] { "[referencedExcluded]*"}, "rsexclude"));
        }

        [Test]
        public async Task Should_Get_Settings_With_ExcludeByFile_From_CoverageProject_And_RunSettings()
        {
            var projectExcludeByFile = new string[] { "excludedByFile" };
            mockCoverageProject.Setup(cp => cp.Settings.ExcludeByFile).Returns(projectExcludeByFile);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");

            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.ExcludeByFile).Returns("rsexcludeByFile");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithExcludeByFile(projectExcludeByFile, "rsexcludeByFile"));
        }

        [Test]
        public async Task Should_Get_Settings_With_ExcludeByAttribute_From_CoverageProject_And_RunSettings()
        {
            var projectExcludeByAttribute = new string[] { "excludedByAttribute" };
            mockCoverageProject.Setup(cp => cp.Settings.ExcludeByAttribute).Returns(projectExcludeByAttribute);
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.ExcludeByAttribute).Returns("rsexcludeByAttribute");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithExcludeByAttribute(projectExcludeByAttribute, "rsexcludeByAttribute"));
        }

        [Test]
        public async Task Should_Get_Settings_With_Include_From_CoverageProject_And_RunSettings()
        {
            var projectInclude= new string[] { "included" };
            mockCoverageProject.Setup(cp => cp.Settings.Include).Returns(projectInclude);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");

            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.Include).Returns("rsincluded");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithInclude(projectInclude, "rsincluded"));
        }

        [TestCase(true,"true")]
        [TestCase(false, "false")]
        public async Task Should_Get_Settings_With_IncludeTestAssembly_From_CoverageProject_And_RunSettings(bool projectIncludeTestAssembly, string runSettingsIncludeTestAssembly)
        {
            mockCoverageProject.Setup(cp => cp.Settings.IncludeTestAssembly).Returns(projectIncludeTestAssembly);
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.IncludeTestAssembly).Returns(runSettingsIncludeTestAssembly);
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithIncludeTestAssembly(projectIncludeTestAssembly, runSettingsIncludeTestAssembly));
        }

        [Test]
        public async Task Should_Initialize_With_Options_And_Run_Settings_First()
        {
            var settings = new Mock<IAppOptions>().Object;
            mockCoverageProject.Setup(cp => cp.RunSettingsFile).Returns(".runsettings");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("output");
            mockCoverageProject.Setup(cp => cp.Settings).Returns(settings);
            
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.Initialize(settings, ".runsettings",Path.Combine("output","FCC.runsettings")));
            
            var invocations = mockDataCollectorSettingsBuilder.Invocations.GetEnumerator().ToIEnumerable().ToList();
            Assert.AreEqual(invocations.First().Method.Name, nameof(IDataCollectorSettingsBuilder.Initialize));
        }

        [Test]
        public async Task Should_Get_Settings_With_ResultsDirectory()
        {
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("outputfolder");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithResultsDirectory("outputfolder"));
        }

        [Test]
        public async Task Should_Get_Settings_With_Blame()
        {
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithBlame());
        }

        [Test]
        public async Task Should_Get_Settings_With_NoLogo()
        {
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithNoLogo());
        }

        [Test]
        public async Task Should_Get_Settings_With_Diagnostics()
        {
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("outputfolder");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithDiagnostics("outputfolder/diagnostics.log"));
        }

        [Test]
        public async Task Should_Get_Settings_With_IncludeDirectory_From_RunSettings()
        {
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.IncludeDirectory).Returns("includeDirectory");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithIncludeDirectory("includeDirectory"));
        }

        [Test]
        public async Task Should_Get_Settings_With_SingleHit_From_RunSettings()
        {
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.SingleHit).Returns("true...");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithSingleHit("true..."));
        }

        [Test]
        public async Task Should_Get_Settings_With_UseSourceLink_From_RunSettings()
        {
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.UseSourceLink).Returns("true...");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithUseSourceLink("true..."));
        }

        [Test]
        public async Task Should_Get_Settings_With_SkipAutoProps_From_RunSettings()
        {
            mockRunSettingsCoverletConfiguration.Setup(rsc => rsc.SkipAutoProps).Returns("true...");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mockDataCollectorSettingsBuilder.Verify(b => b.WithSkipAutoProps("true..."));
        }

        [Test]
        public async Task Should_Log_VSTest_Run_With_Settings()
        {
            mockCoverageProject.Setup(cp => cp.ProjectName).Returns("TestProject");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockDataCollectorSettingsBuilder.Setup(sb => sb.Build()).Returns("settings string");
            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            mocker.Verify<ILogger>(l => l.Log(coverletDataCollectorUtil.LogRunMessage("settings string")));
        }

        [Test]
        public async Task Should_Execute_DotNet_Test_Collect_XPlat_With_Settings_Using_The_ProcessUtil()
        {
            mockCoverageProject.Setup(cp => cp.ProjectOutputFolder).Returns("projectOutputFolder");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockDataCollectorSettingsBuilder.Setup(sb => sb.Build()).Returns("settings");
            coverletDataCollectorUtil.TestAdapterPathArg = "testadapterpath";
            var ct = CancellationToken.None;
            await coverletDataCollectorUtil.RunAsync(ct);
            mocker.Verify<IProcessUtil>(p => p.ExecuteAsync(It.Is<ExecuteRequest>(er => er.Arguments == @"test --collect:""XPlat Code Coverage"" settings --test-adapter-path testadapterpath" && er.FilePath == "dotnet" && er.WorkingDirectory == "projectOutputFolder"),ct));
        }

        private async Task<CancellationToken> Use_Custom_TestAdapterPath()
        {
            CreateTemporaryDirectory();
            mockCoverageProject.Setup(cp => cp.ProjectOutputFolder).Returns("projectOutputFolder");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            mockCoverageProject.Setup(cp => cp.Settings.CoverletCollectorDirectoryPath).Returns(tempDirectory);
            mockDataCollectorSettingsBuilder.Setup(sb => sb.Build()).Returns("settings");
            coverletDataCollectorUtil.TestAdapterPathArg = "testadapterpath";
            var ct = CancellationToken.None;
            await coverletDataCollectorUtil.RunAsync(ct);
            return ct;
        }

        [Test]
        public async Task Should_Use_Custom_TestAdapterPath_Quoted_If_Specified_In_Settings_And_Exists()
        {
            var ct = await Use_Custom_TestAdapterPath();
            mocker.Verify<IProcessUtil>(p => p.ExecuteAsync(It.Is<ExecuteRequest>(er => er.Arguments == $@"test --collect:""XPlat Code Coverage"" settings --test-adapter-path ""{tempDirectory}""" && er.FilePath == "dotnet" && er.WorkingDirectory == "projectOutputFolder"),ct));
        }

        [Test]
        public async Task Should_Log_When_Using_Custom_TestAdapterPath()
        {
            await Use_Custom_TestAdapterPath();
            mocker.Verify<ILogger>(l => l.Log($"Using custom coverlet data collector : {tempDirectory}"));
        }

        [Test]
        public async Task Should_Use_The_ProcessResponseProcessor()
        {
            mockCoverageProject.Setup(cp => cp.ProjectName).Returns("TestProject");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("");
            
            var mockProcesUtil = mocker.GetMock<IProcessUtil>();
            var executeResponse = new ExecuteResponse();
            var ct = CancellationToken.None;
            mockProcesUtil.Setup(p => p.ExecuteAsync(It.IsAny<ExecuteRequest>(), ct).Result).Returns(executeResponse);
            var mockProcessResponseProcessor = mocker.GetMock<IProcessResponseProcessor>();

            var logTitle = "Coverlet Collector Run (TestProject)";
            mockProcessResponseProcessor.Setup(rp => rp.Process(executeResponse, It.IsAny<Func<int, bool>>(), true, logTitle, It.IsAny<Action>()));

            await coverletDataCollectorUtil.RunAsync(ct);
            mockProcessResponseProcessor.VerifyAll();
        }

        [TestCase(2, false)]
        [TestCase(1,true)]
        [TestCase(0, true)]
        public async Task Should_Only_Be_Successful_With_ExitCode_0_Or_1(int exitCode, bool expectedSuccess)
        {
            var mockProcessResponseProcessor = mocker.GetMock<IProcessResponseProcessor>();
            Func<int, bool> _exitCodePredicate = null;
            mockProcessResponseProcessor.Setup(rp => rp.Process(It.IsAny<ExecuteResponse>(), It.IsAny<Func<int, bool>>(), true, It.IsAny<string>(), It.IsAny<Action>())).Callback<ExecuteResponse, Func<int, bool>, bool, string, Action>((_, exitCodePredicate, __, ___, ____) =>
                {
                    _exitCodePredicate = exitCodePredicate;
                });

            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            Assert.AreEqual(expectedSuccess, _exitCodePredicate(exitCode));
        }

        [Test]
        public async Task Should_Correct_The_CoberturaPath_Given_Successful_Execution()
        {
            mockCoverageProject.Setup(cp => cp.CoverageOutputFolder).Returns("outputFolder");
            mockCoverageProject.Setup(cp => cp.CoverageOutputFile).Returns("outputFile");
            var mockProcessResponseProcessor = mocker.GetMock<IProcessResponseProcessor>();
            Action _successCallback = null;
            mockProcessResponseProcessor.Setup(rp => rp.Process(It.IsAny<ExecuteResponse>(), It.IsAny<Func<int, bool>>(), true, It.IsAny<string>(), It.IsAny<Action>())).Callback<ExecuteResponse, Func<int, bool>, bool, string, Action>((_, __, ___, ____, successCallback) =>
            {
                _successCallback = successCallback;
            });

            await coverletDataCollectorUtil.RunAsync(CancellationToken.None);
            _successCallback();
            mocker.Verify<ICoverletDataCollectorGeneratedCobertura>(gc => gc.CorrectPath("outputFolder", "outputFile"));
        }
    }

    public static class IEnumeratorExtensions {
        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        } }

}