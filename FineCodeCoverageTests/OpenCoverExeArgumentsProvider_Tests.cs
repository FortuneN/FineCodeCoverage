using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests
{
    internal class OpenCoverExeArgumentsProvider_Tests
    {
        [Test]
        public void Should_MergeByHash()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments =  openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasSetting(arguments, "-mergebyhash");
        }

        [Test]
        public void Should_HideSkippedAll()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasSetting(arguments, "-hideskipped:all");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Register_Depending_Upon_Project_Is64Bit(bool is64Bit)
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Is64Bit).Returns(is64Bit);

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasSetting(arguments, is64Bit ? "-register:path64" : "-register:path32");
        }

        [Test]
        public void Should_Safely_Set_Target_To_MsTestPlatformExePath()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "msTestPlatformExePath");
            
            AssertHasEscapedSetting(arguments, "-target:msTestPlatformExePath");
        }

        [Test]
        public void Should_Safely_Include_The_TestDLLFile_In_The_TargetArgs()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("testDllFile");
            
            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, $"-targetargs:{OpenCoverExeEscaper.EscapeQuoteTargetArgsArgument("testDllFile")}");
        }

        [Test]
        public void Should_Safely_Include_The_Project_RunSettingsFile_In_The_TargetArgs_When_Present()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("testDllFile");
            mockCoverageProject.SetupGet(coverageProject => coverageProject.RunSettingsFile).Returns("runSettingsFile");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, $"-targetargs:{OpenCoverExeEscaper.EscapeQuoteTargetArgsArgument("testDllFile")} /Settings:{OpenCoverExeEscaper.EscapeQuoteTargetArgsArgument("runSettingsFile")}");
        }

        [Test]
        public void Should_Safely_Output_To_The_Project_CoverageOutputFile()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.CoverageOutputFile).Returns("coverageOutputFile");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasEscapedSetting(arguments, "-output:coverageOutputFile");
        }

        [Test]
        public void Should_Safely_ExcludeByFile_SemilColon_Delimited_Project_ExcludeByFile_Entries_Trimmed_Of_Spaces_And_Quotes()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.ExcludeByFile).Returns(new string[] { 
                @"  ""excludeByFile1""  ",
                " 'excludeByFile2' ",
            });

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, "-excludebyfile:excludeByFile1;excludeByFile2");
        }

        [Test]
        public void Should_Not_Throw_If_Project_ExcludeByFile_Is_Null()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.ExcludeByFile).Returns((string[])null);

            Assert.DoesNotThrow(() => openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, ""));
        }

        [Test]
        public void Should_Not_Add_ExcludeByFile_If_There_Are_None()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            AssertDoesNotHaveSetting(arguments, "excludebyfile");
        }

        private Mock<ICoverageProject> SafeMockCoverageProject()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.IncludedReferencedProjects).Returns(new List<string>());
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ExcludedReferencedProjects).Returns(new List<string>());
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings).Returns(new Mock<IAppOptions>().Object);
            return mockCoverageProject;
        }

        private void AssertDoesNotHaveSetting(List<string> openCoverSettings, string setting)
        {
            Assert.IsFalse(openCoverSettings.Any(openCoverSetting => openCoverSetting.Contains($"-{setting}:")));
        }

        private void AssertHasSetting(List<string> openCoverSettings, string setting)
        {
            Assert.IsTrue(openCoverSettings.Any(openCoverSetting => openCoverSetting == setting));
        }

        private void AssertHasEscapedSetting(List<string> openCoverSettings, string setting)
        {
            AssertHasSetting(openCoverSettings, OpenCoverExeEscaper.EscapeArgument(setting));
        }
    }
}
