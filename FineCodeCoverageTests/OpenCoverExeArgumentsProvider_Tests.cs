using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Experimentation;
using Moq;
using NUnit.Framework;
using System;
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
        public void Should_Register_Depending_Upon_Project_Is64Bit_When_Project_OpenCoverRegister_Is_Default(bool is64Bit)
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Is64Bit).Returns(is64Bit);

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasSetting(arguments, is64Bit ? "-register:path64" : "-register:path32");
        }

        [TestCase(OpenCoverRegister.User,":user")]
        [TestCase(OpenCoverRegister.Path32,":path32")]
        [TestCase(OpenCoverRegister.Path64,":path64")]
        [TestCase(OpenCoverRegister.NoArg,"")]
        public void Should_Register_Using_Project_OpenCoverRegister_When_Not_Default(OpenCoverRegister register, string expectedSuffix)
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.OpenCoverRegister).Returns(register);

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasSetting(arguments, $"-register{expectedSuffix}");
        }

        [Test]
        public void Should_Safely_Set_Target_To_MsTestPlatformExePath_When_Not_Provided_In_Project_Settings()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "msTestPlatformExePath");
            
            AssertHasEscapedSetting(arguments, "-target:msTestPlatformExePath");
        }

        [Test]
        public void Should_Safely_Set_Target_From_Project_Settings_When_Provided()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.Setup(coverageProject => coverageProject.Settings.OpenCoverTarget).Returns("openCoverTarget");
            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "msTestPlatformExePath");

            AssertHasEscapedSetting(arguments, "-target:openCoverTarget");
        }

        [Test]
        public void Should_Safely_Include_The_TestDLLFile_In_The_TargetArgs()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("testDllFile");
            
            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, $"-targetargs:{CommandLineArguments.AddEscapeQuotes("testDllFile")}");
        }

        [Test]
        public void Should_Include_The_Test_Assembly_In_The_Filter_When_AppOptions_IncludeTestAssembly_And_Required()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(true);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.Include).Returns(new string[] { "[anassembly]*"});
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ProjectName).Returns("TheTestName");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            var filters = GetFilters(arguments);
            Assert.That(filters, Is.EquivalentTo(new string[] { "+[TheTestName]*", "+[anassembly]*" }));
        }

        [Test]
        public void Should_Not_Include_The_Test_Assembly_In_The_Filter_When_AppOptions_IncludeTestAssembly_And_No_Other_Includes()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(true);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ProjectName).Returns("TheTestName");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            var filters = GetFilters(arguments);
            Assert.IsEmpty(GetFilters(arguments));
        }

        [Test]
        public void Should_Not_Include_The_Test_Assembly_In_The_Filter_When_AppOptions_IncludeTestAssembly_And_Explicitly_Excluded()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.IncludeTestAssembly).Returns(true);
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.Include).Returns(new string[] { "[anassembly]*" });
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.Exclude).Returns(new string[] { "[TheTestName]*" });
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ProjectName).Returns("TheTestName");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            var filters = GetFilters(arguments);
            Assert.That(filters, Is.EquivalentTo(new string[] { "+[anassembly]*", "-[TheTestName]*" }));
        }

        private IEnumerable<string> GetFilters(IEnumerable<string> arguments)
        {
            var filterMatch = "-filter:";
            var filter = arguments.FirstOrDefault(arg => arg.StartsWith($@"""{filterMatch}"));
            if (filter == null)
            {
                return Enumerable.Empty<string>();
            }
            if (!filter.EndsWith("\""))
            {
                throw new Exception("filter should be escaped");
            }
            return filter.Replace("\"", "").Substring(filterMatch.Length).Split(' ');
        }

        [Test]
        public void Should_Safely_Include_The_Project_RunSettingsFile_In_The_TargetArgs_When_Present()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("testDllFile");
            mockCoverageProject.SetupGet(coverageProject => coverageProject.RunSettingsFile).Returns("runSettingsFile");

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, $"-targetargs:{CommandLineArguments.AddEscapeQuotes("testDllFile")} /Settings:{CommandLineArguments.AddEscapeQuotes("runSettingsFile")}");
        }

        [Test]
        public void Should_Safely_Include_The_Project_OpenCoverTargetArgs_When_Present()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.TestDllFile).Returns("testDllFile");
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.OpenCoverTargetArgs).Returns("openCoverAdditionalTargetArgs");
            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, $"-targetargs:{CommandLineArguments.AddEscapeQuotes("testDllFile")} openCoverAdditionalTargetArgs");
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
        public void Should_Safely_ExcludeByAttribute_Ordered_SemilColon_Delimited_Default_ExcludeFromCodeCoverage()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, "-excludebyattribute:*.ExcludeFromCodeCoverage;*.ExcludeFromCodeCoverageAttribute;*.ExcludeFromCoverage;*.ExcludeFromCoverageAttribute");
        }

        [Test]
        public void Should_Safely_ExcludeByAttribute_Ordered_SemilColon_Delimited_Project_ExcludeByAttribute_With_Wildcard_For_Short_Name()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.ExcludeByAttribute).Returns(new string[] {
                @"  ""ShortFormAttribute""  ",
                " 'Long.Form' ",
            });

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, "-excludebyattribute:*.ExcludeFromCodeCoverage;*.ExcludeFromCodeCoverageAttribute;*.ExcludeFromCoverage;*.ExcludeFromCoverageAttribute;Long.Form;Long.FormAttribute;*.ShortForm;*.ShortFormAttribute");
        }

        [Test]
        public void Should_Not_Throw_If_Project_ExcludeByAttribute_Is_Null()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.Settings.ExcludeByAttribute).Returns((string[])null);

            Assert.DoesNotThrow(() => openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, ""));
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

        [Test]
        public void Should_Safely_Filter_Include_Project_IncludedReferencedProjects_Space_Delimited()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.Setup(coverageProject => coverageProject.IncludedReferencedProjects)
                .Returns(new List<IReferencedProject> { new ReferencedProject("", "Referenced1", true), new ReferencedProject("", "Referenced2", true) });
            
            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");
            
            AssertHasEscapedSetting(arguments, "-filter:+[Referenced1]* +[Referenced2]*");
        }

        [Test]
        public void Should_Safely_Filter_Include_All_When_Exclude_And_No_Include()
        {
            Should_Safely_Filter_Exclude_Project_ExcludedReferencedProjects_Space_Delimited();
        }

        [Test]
        public void Should_Safely_Filter_Exclude_Project_ExcludedReferencedProjects_Space_Delimited()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.Setup(coverageProject => coverageProject.ExcludedReferencedProjects)
                .Returns(new List<IReferencedProject> { new ReferencedProject("", "Referenced1", true), new ReferencedProject("", "Referenced2", true) });

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, "-filter:+[*]* -[Referenced1]* -[Referenced2]*");
        }

        [Test]
        public void Should_Safely_Filter_Exclude_Trimmed_Project_Excludes_Trimmed_Of_Spaces_And_Quotes_Space_Delimited()
        {
            var openCoverExeArgumentsProvider = new OpenCoverExeArgumentsProvider();
            var mockCoverageProject = SafeMockCoverageProject();
            mockCoverageProject.Setup(coverageProject => coverageProject.Settings.Exclude)
                .Returns(new string[] { "  '[Exclude1]*'  ", "  \"[Exclude2]* \" " });

            var arguments = openCoverExeArgumentsProvider.Provide(mockCoverageProject.Object, "");

            AssertHasEscapedSetting(arguments, "-filter:+[*]* -[Exclude1]* -[Exclude2]*");
        }

        private Mock<ICoverageProject> SafeMockCoverageProject()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(coverageProject => coverageProject.IncludedReferencedProjects).Returns(new List<IReferencedProject>());
            mockCoverageProject.SetupGet(coverageProject => coverageProject.ExcludedReferencedProjects).Returns(new List<IReferencedProject>());
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
            AssertHasSetting(openCoverSettings, CommandLineArguments.AddQuotes(setting));
        }
    }
}
