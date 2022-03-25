using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System;
using AutoMoq;
using FineCodeCoverage.Engine.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using System.Linq;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class TemplatedRunSettingsService_Tests
    {
        private AutoMoqer autoMocker;
        private TemplatedRunSettingsService templatedRunSettingsService;

        [SetUp]
        public void SetupSut()
        {
            autoMocker = new AutoMoqer();
            templatedRunSettingsService = autoMocker.Create<TemplatedRunSettingsService>();
        }

        [Test]
        public async Task Should_Create_Run_Settings_From_Template()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            var coverageProject = mockCoverageProject.Object;
            var coverageProjects = new List<ICoverageProject> { coverageProject};

            var mockRunSettingsTemplate = autoMocker.GetMock<IRunSettingsTemplate>();
            mockRunSettingsTemplate.Setup(runSettingsTemplate => runSettingsTemplate.ToString()).Returns("<MockRunSettingsTemplate/>");

            var runSettingsTemplateReplacements = new RunSettingsTemplateReplacements();
            var mockRunSettingsTemplateReplacementFactory = autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>();
            mockRunSettingsTemplateReplacementFactory.Setup(
                runSettingsTemplateReplacementsFactory =>
                runSettingsTemplateReplacementsFactory.Create(coverageProject, "FccTestAdapterPath")
            ).Returns(runSettingsTemplateReplacements);

            var result = await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");

            mockRunSettingsTemplate.Verify(
                runSettingsTemplate => runSettingsTemplate.ReplaceTemplate(
                    "<MockRunSettingsTemplate/>",
                    runSettingsTemplateReplacements, It.IsAny<bool>())
            );

        }

        [Test]
        public async Task Should_Create_Run_Settings_From_Configured_Custom_Template_If_Available()
        {
            Mock<ICustomRunSettingsTemplateProvider> mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            mockCustomRunSettingsTemplateProvider.Setup(
                customRunSettingsTemplateProvider =>
                customRunSettingsTemplateProvider.Provide(@"C:\SomeProject", "SolutionDirectory")
            ).Returns(new CustomRunSettingsTemplateDetails { Path = "Custom path", Template = "<CustomTemplate/>" });
            var runSettingsTemplateReplacements = SetupReplacements();

            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns(@"C:\SomeProject\SomeProject.csproj");
            var coverageProject = mockCoverageProject.Object;
            var coverageProjects = new List<ICoverageProject> { coverageProject };

            var mockRunSettingsTemplate = autoMocker.GetMock<IRunSettingsTemplate>();
            mockRunSettingsTemplate.Setup(runSettingsTemplate => runSettingsTemplate.ConfigureCustom("<CustomTemplate/>")).Returns("<ConfiguredCustom/>");

            var result = await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");

            
            mockRunSettingsTemplate.Verify(
                runSettingsTemplate => runSettingsTemplate.ReplaceTemplate(
                    "<ConfiguredCustom/>",
                    runSettingsTemplateReplacements, It.IsAny<bool>())
            );
        }

        [Test]
        public async Task Should_Return_ExceptionReason_Result_If_Throws_Creating_RunSettings()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns(@"C:\SomeProject\SomeProject.csproj");
            var coverageProject = mockCoverageProject.Object;
            var coverageProjects = new List<ICoverageProject> { coverageProject };

            var exception = new Exception("The message");
            SetupICustomRunSettingsTemplateProviderAllIsAny().Throws(exception);

            var result = await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");
            var exceptionReason = result.ExceptionReason;
            Assert.AreSame(exception, exceptionReason.Exception);
            Assert.AreEqual("Exception generating runsettings from template", exceptionReason.Reason);
        }

        [Test]
        public async Task Should_Write_Generated_RunSettings()
        {
            SetupReplaceResult(new TemplateReplaceResult { Replaced = "RunSettings" });

            var coverageProjects = CreateCoverageProjectsSingle();
            await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");
            
            var coverageProjectRunSettings = GetWriteProjectsRunSettingsAsyncArgument().Single();
            Assert.AreEqual("RunSettings", coverageProjectRunSettings.RunSettings);
            Assert.AreEqual(coverageProjects.Single(), coverageProjectRunSettings.CoverageProject);
        }

        [Test]
        public async Task Should_Return_ExceptionReason_Result_If_Throws_Writing_Generated_RunSettings()
        {
            SetupReplaceResult(new TemplateReplaceResult { Replaced = "RunSettings" });

            var exception = new Exception();
            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            mockProjectRunSettingsGenerator.Setup(
                projectRunSettingsGenerator =>
                projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(It.IsAny<IEnumerable<ICoverageProjectRunSettings>>())
            ).ThrowsAsync(exception);

            var coverageProjects = CreateCoverageProjectsSingle();
            var result = await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");
            var exceptionReason = result.ExceptionReason;
            Assert.AreSame(exception, exceptionReason.Exception);
            Assert.AreEqual("Exception writing templated runsettings", exceptionReason.Reason);
        }

        [Test]
        public async Task Should_Return_A_Result_With_No_ExceptionReason_When_No_Exception()
        {
            var mockCoverageProject1 = new Mock<ICoverageProject>();
            mockCoverageProject1.Setup(cp => cp.ProjectFile).Returns(@"C:\SomeProject\SomeProject.csproj");
            var mockCoverageProject2 = new Mock<ICoverageProject>();
            mockCoverageProject2.Setup(cp => cp.ProjectFile).Returns(@"C:\SomeProject2\SomeProject2.csproj");
            var coverageProjects = new List<ICoverageProject>
            {
                mockCoverageProject1.Object,
                mockCoverageProject2.Object
            };

            var mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            mockCustomRunSettingsTemplateProvider.SetupSequence(
                customRunSettingsTemplateProvider => customRunSettingsTemplateProvider.Provide(It.IsAny<string>(), It.IsAny<string>())
                ).Returns(new CustomRunSettingsTemplateDetails { Path = "Custom template path" })
                .Returns((CustomRunSettingsTemplateDetails)null);

            var mockRunSettingsTemplate = autoMocker.GetMock<IRunSettingsTemplate>();
            mockRunSettingsTemplate.SetupSequence(
                runSettingsTemplate =>
                runSettingsTemplate.ReplaceTemplate(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>(), It.IsAny<bool>())
            ).Returns(
                new TemplateReplaceResult
                {
                    Replaced = "RunSettings1",
                    ReplacedTestAdapter = false
                }
            ).Returns(
                new TemplateReplaceResult
                {
                    Replaced = "RunSettings2",
                    ReplacedTestAdapter = true
                }
            );

            var result = await templatedRunSettingsService.GenerateAsync(coverageProjects, "SolutionDirectory", "FccTestAdapterPath");
            Assert.Null(result.ExceptionReason);
            Assert.AreEqual(new List<string> { "Custom template path" }, result.CustomTemplatePaths);
            Assert.AreEqual(new List<ICoverageProject> { coverageProjects[1]}, result.CoverageProjectsWithFCCMsTestAdapter);


        }

        [Test]
        public async Task Clean_Up_Should_Remove_Generated_Project_RunSettings()
        {
            var coverageProjects = new List<ICoverageProject> { new Mock<ICoverageProject>().Object};
            await templatedRunSettingsService.CleanUpAsync(coverageProjects);
            autoMocker.Verify<IProjectRunSettingsGenerator>(
                projectRunSettingsGenerator => projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(coverageProjects)
            );
        }

        private Moq.Language.Flow.ISetup<ICustomRunSettingsTemplateProvider, CustomRunSettingsTemplateDetails> SetupICustomRunSettingsTemplateProviderAllIsAny()
        {
            var mockCustomRunSettingsTemplateProvider = autoMocker.GetMock<ICustomRunSettingsTemplateProvider>();
            return mockCustomRunSettingsTemplateProvider.Setup(customRunSettingsTemplateProvider =>
                customRunSettingsTemplateProvider.Provide(It.IsAny<string>(), It.IsAny<string>())
            );
        }

        private List<ICoverageProject> CreateCoverageProjectsSingle()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns(@"C:\SomeProject\SomeProject.csproj");
            var coverageProject = mockCoverageProject.Object;
            return new List<ICoverageProject> { coverageProject };
        }

        private void SetupReplaceResult(ITemplateReplacementResult templateReplacementResult)
        {
            var mockRunSettingsTemplate = autoMocker.GetMock<IRunSettingsTemplate>();
            mockRunSettingsTemplate.Setup(
                runSettingsTemplate =>
                runSettingsTemplate.ReplaceTemplate(It.IsAny<string>(), It.IsAny<IRunSettingsTemplateReplacements>(), It.IsAny<bool>())
            ).Returns(templateReplacementResult);
        }

        private IEnumerable<ICoverageProjectRunSettings> GetWriteProjectsRunSettingsAsyncArgument()
        {
            var mockProjectRunSettingsGenerator = autoMocker.GetMock<IProjectRunSettingsGenerator>();
            return mockProjectRunSettingsGenerator.Invocations
                .Single(invocation => invocation.Method.Name == nameof(IProjectRunSettingsGenerator.WriteProjectsRunSettingsAsync))
                .Arguments[0] as IEnumerable<ICoverageProjectRunSettings>;
        }

        private IRunSettingsTemplateReplacements SetupReplacements()
        {
            var runSettingsTemplateReplacements = new RunSettingsTemplateReplacements();
            var mockRunSettingsTemplateReplacementFactory = autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>();
            mockRunSettingsTemplateReplacementFactory.Setup(
                runSettingsTemplateReplacementsFactory =>
                runSettingsTemplateReplacementsFactory.Create(It.IsAny<ICoverageProject>(), It.IsAny<string>())
            ).Returns(runSettingsTemplateReplacements);
            return runSettingsTemplateReplacements;
        }
    }

}
