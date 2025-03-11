using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using FineCodeCoverage.Options;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Linq;
using FineCodeCoverage.Engine.Model;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class TestMsCodeCoverageOptions : IMsCodeCoverageOptions
    {
        public string[] ModulePathsExclude { get; set; }
        public string[] ModulePathsInclude { get; set; }
        public string[] CompanyNamesExclude { get; set; }
        public string[] CompanyNamesInclude { get; set; }
        public string[] PublicKeyTokensExclude { get; set; }
        public string[] PublicKeyTokensInclude { get; set; }
        public string[] SourcesExclude { get; set; }
        public string[] SourcesInclude { get; set; }
        public string[] AttributesExclude { get; set; }
        public string[] AttributesInclude { get; set; }
        public string[] FunctionsInclude { get; set; }
        public string[] FunctionsExclude { get; set; }

        public bool Enabled { get; set; }

        public bool IncludeTestAssembly { get; set; }
        public bool IncludeReferencedProjects { get; set; }
        public string[] ExcludeAssemblies { get; set; }
        public string[] IncludeAssemblies { get; set; }
        public bool DisabledNoCoverage { get; set; }
    }

    internal static class ReplacementsAssertions
    {
        public static void AssertAllEmpty(IRunSettingsTemplateReplacements replacements)
        {
            Assert.IsEmpty(replacements.ModulePathsExclude);
            Assert.IsEmpty(replacements.ModulePathsInclude);
            Assert.IsEmpty(replacements.AttributesExclude);
            Assert.IsEmpty(replacements.AttributesInclude);
            Assert.IsEmpty(replacements.FunctionsExclude);
            Assert.IsEmpty(replacements.FunctionsInclude);
            Assert.IsEmpty(replacements.CompanyNamesExclude);
            Assert.IsEmpty(replacements.CompanyNamesInclude);
            Assert.IsEmpty(replacements.PublicKeyTokensExclude);
            Assert.IsEmpty(replacements.PublicKeyTokensInclude);
            Assert.IsEmpty(replacements.SourcesExclude);
            Assert.IsEmpty(replacements.SourcesInclude);
        }
    }


    internal class RunSettingsTemplateReplacementsFactory_UserRunSettings_Tests
    {
        private RunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory;

        private class TestUserRunSettingsProjectDetails : IUserRunSettingsProjectDetails
        {
            public List<IReferencedProject> ExcludedReferencedProjects { get; set; }
            public List<IReferencedProject> IncludedReferencedProjects { get; set; }
            public string CoverageOutputFolder { get; set; }
            public IMsCodeCoverageOptions Settings { get; set; }
            public string TestDllFile { get; set; }
        }

        [SetUp]
        public void CreateSut()
        {
            runSettingsTemplateReplacementsFactory = new RunSettingsTemplateReplacementsFactory();
        }

        [Test]
        public void Should_Set_The_TestAdapter()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
            };
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, "ms-test-adapter-path");
            Assert.AreEqual("ms-test-adapter-path", replacements.TestAdapter);
        }

        [TestCase("1", "2")]
        [TestCase("2", "1")]
        public void Should_Set_The_ResultsDirectory_To_The_First_OutputFolder(string outputFolder1, string outputFolder2)
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2")
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = outputFolder1,
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = outputFolder2,
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Other",
                    new TestUserRunSettingsProjectDetails
                    {

                    }
                }
            };
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);
            Assert.AreEqual(outputFolder1, replacements.ResultsDirectory);
        }

        [Test]
        public void Should_Have_Includes_And_Excludes_From_All_Coverage_Projects()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2")
            };

            TestMsCodeCoverageOptions CreateSettings(string id)
            {
                return new TestMsCodeCoverageOptions
                {
                    IncludeTestAssembly = true, // not testing ModulePaths here

                    AttributesExclude = new string[] { $"AttributeExclude{id}" },
                    AttributesInclude = new string[] { $"AttributeInclude{id}" },
                    CompanyNamesExclude = new string[] { $"CompanyNameExclude{id}" },
                    CompanyNamesInclude = new string[] { $"CompanyNameInclude{id}" },
                    FunctionsExclude = new string[] { $"FunctionExclude{id}" },
                    FunctionsInclude = new string[] { $"FunctionInclude{id}" },
                    PublicKeyTokensExclude = new string[] { $"PublicKeyTokenExclude{id}" },
                    PublicKeyTokensInclude = new string[] { $"PublicKeyTokenInclude{id}" },
                    SourcesExclude = new string[] { $"SourceExclude{id}" },
                    SourcesInclude = new string[] { $"SourceInclude{id}" },
                };
            }

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = CreateSettings("1"),
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = CreateSettings("2"),
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Other",
                    new TestUserRunSettingsProjectDetails
                    {

                    }
                }
            };
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.FunctionsExclude, "Function", new[] { "FunctionExclude1", "FunctionExclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.FunctionsInclude, "Function", new[] { "FunctionInclude1", "FunctionInclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.CompanyNamesExclude, "CompanyName", new[] { "CompanyNameExclude1", "CompanyNameExclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.CompanyNamesInclude, "CompanyName", new[] { "CompanyNameInclude1", "CompanyNameInclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.AttributesExclude, "Attribute", new[] { "AttributeExclude1", "AttributeExclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.AttributesInclude, "Attribute", new[] { "AttributeInclude1", "AttributeInclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.PublicKeyTokensExclude, "PublicKeyToken", new[] { "PublicKeyTokenExclude1", "PublicKeyTokenExclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.PublicKeyTokensInclude, "PublicKeyToken", new[] { "PublicKeyTokenInclude1", "PublicKeyTokenInclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.SourcesExclude, "Source", new[] { "SourceExclude1", "SourceExclude2" });
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.SourcesInclude, "Source", new[] { "SourceInclude1", "SourceInclude2" });
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Should_Add_The_Test_Assembly_Regex_Escaped_To_Module_Excludes_When_IncludeTestAssembly_Is_False(bool includeTestAssembly1, bool includeTestAssembly2)
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{
                            IncludeTestAssembly = includeTestAssembly1,
                        },
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                        TestDllFile = @"Some\Path1"
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = includeTestAssembly2},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                        TestDllFile = @"Some\Path2"
                    }
                },
            };

            var testDlls = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value.TestDllFile).ToList();
            string GetModulePathExcludeWhenExcludingTestAssembly(bool first)
            {
                return MsCodeCoverageRegex.RegexEscapePath(testDlls[first ? 0 : 1]);
            }
            var expectedModulePathExcludes = new List<string>();
            if (!includeTestAssembly1)
            {
                expectedModulePathExcludes.Add(GetModulePathExcludeWhenExcludingTestAssembly(true));
            }
            if (!includeTestAssembly2)
            {
                expectedModulePathExcludes.Add(GetModulePathExcludeWhenExcludingTestAssembly(false));
            }
            
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.ModulePathsExclude, "ModulePath", expectedModulePathExcludes);
        }

        [Test]
        public void Should_Add_Regexed_ExcludedReferencedProjects_And_ModulePathsExclude_To_ModulePaths()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions
                        {
                            ModulePathsExclude = new string[] { "ModulePathExclude1" },
                            IncludeTestAssembly = true
                        },
                        ExcludedReferencedProjects = new List<IReferencedProject> { new ReferencedProject("", "ExcludedReferenced1", false) },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions {
                            ModulePathsExclude = new string[] { "ModulePathExclude2" },
                            IncludeTestAssembly = true
                        },
                        ExcludedReferencedProjects = new List<IReferencedProject> { new ReferencedProject("", "ExcludedReferenced2", true) },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
            };

            var projectDetails = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value).ToList();
            IEnumerable<string> expectedExcludedReferencedProjects = projectDetails.SelectMany(pd => pd.ExcludedReferencedProjects)
                .Select(rp => MsCodeCoverageRegex.RegexModuleName(rp.AssemblyName, rp.IsDll));

            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsExclude, 
                "ModulePath", 
                new[] { "ModulePathExclude1", "ModulePathExclude2" }.Concat(expectedExcludedReferencedProjects));
            
        }

        [Test]
        public void Should_Add_Regexed_InludedReferencedProjects_And_ModulePathsInclude_To_ModulePaths()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions
                        {
                            ModulePathsInclude = new string[] { "ModulePathInclude1" },
                            IncludeTestAssembly = false
                        },
                        IncludedReferencedProjects = new List<IReferencedProject> { new ReferencedProject("", "InludedReferenced1", false) },
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions {
                            ModulePathsInclude = new string[] { "ModulePathInclude2" },
                            IncludeTestAssembly = false
                        },
                        IncludedReferencedProjects = new List<IReferencedProject> { new ReferencedProject("", "IncludedReferenced2", true) },
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
            };

            var projectDetails = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value).ToList();
            IEnumerable<string> expectedIncludedReferencedProjects = projectDetails.SelectMany(pd => pd.IncludedReferencedProjects)
                .Select(rp => MsCodeCoverageRegex.RegexModuleName(rp.AssemblyName, rp.IsDll));

            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                new[] { "ModulePathInclude1", "ModulePathInclude2" }.Concat(expectedIncludedReferencedProjects));
        }

        [Test]
        public void Should_Add_TestAssembly_To_ModulePathsInclude_When_Other_Includes_And_IncludeTestAssembly_True()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = @"Some\Path1",
                        Settings = new TestMsCodeCoverageOptions
                        {
                            ModulePathsInclude = new string[] { "ModulePathInclude1" },
                            IncludeTestAssembly = true,
                            
                        },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList(),
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = @"Some\Path2",
                        Settings = new TestMsCodeCoverageOptions {
                            IncludeTestAssembly = true
                        },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList(),
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
            };

            

            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            var expectedTestDlls = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value.TestDllFile).Select(testDll => MsCodeCoverageRegex.RegexEscapePath(testDll));
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                new[] { "ModulePathInclude1",  }.Concat(expectedTestDlls));
        }

        [Test]
        public void Should_Not_Add_TestAssembly_To_ModulePathsInclude_When_No_Other_Includes_And_IncludeTestAssembly_True()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2"),
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = @"Some\Path1",
                        Settings = new TestMsCodeCoverageOptions
                        {
                            IncludeTestAssembly = true,
                        },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList(),
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = @"Some\Path2",
                        Settings = new TestMsCodeCoverageOptions {
                            IncludeTestAssembly = true
                        },
                        IncludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList(),
                        ExcludedReferencedProjects = Enumerable.Empty<IReferencedProject>().ToList()
                    }
                },
            };



            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);

            var expectedTestDlls = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value.TestDllFile).Select(testDll => MsCodeCoverageRegex.RegexEscapePath(testDll));
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                Enumerable.Empty<string>());
        }

        [Test]
        public void Should_Be_Null_TestAdapter_Replacement_When_Null()
        {
            var testContainers = new List<ITestContainer>()
            {
                CreateTestContainer("Source1"),
                CreateTestContainer("Source2")
            };

            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                }
            };
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);
            Assert.That(replacements.TestAdapter, Is.Null);
        }

        [TestCase(true, true, "true")]
        [TestCase(false, true, "true")]
        [TestCase(true, false, "true")]
        [TestCase(false, false, "false")]
        public void Should_Be_Disabled_When_All_Projects_Are_Disabled(bool project1Enabled, bool project2Enabled, string expectedEnabled)
        {
            var testContainer1 = CreateTestContainer("Source1");
            var testContainer2 = CreateTestContainer("Source2");
            var testContainers = new List<ITestContainer>()
            {
                testContainer1,
                testContainer2
            };
            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                {
                    "Source1",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ Enabled = project1Enabled, IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        TestDllFile = "",
                        Settings = new TestMsCodeCoverageOptions{ Enabled = project2Enabled,  IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<IReferencedProject>(),
                        IncludedReferencedProjects = new List<IReferencedProject>(),
                    }
                }
            };

            var runSettingsTemplateReplacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, "");

            Assert.That(runSettingsTemplateReplacements.Enabled, Is.EqualTo(expectedEnabled));
        }

        private ITestContainer CreateTestContainer(string source)
        {
            var mockTestContainer = new Mock<ITestContainer>();
            mockTestContainer.Setup(tc => tc.Source).Returns(source);
            return mockTestContainer.Object;
        }
    }

    internal class RunSettingsTemplateReplacementsFactory_Template_Tests
    {
        private RunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory;

        [SetUp]
        public void CreateSut()
        {
            runSettingsTemplateReplacementsFactory = new RunSettingsTemplateReplacementsFactory();
        }

        [Test]
        public void Should_Set_The_TestAdapter()
        {
            var replacements = runSettingsTemplateReplacementsFactory.Create(CreateCoverageProject(), "MsTestAdapterPath");
            Assert.AreEqual("MsTestAdapterPath", replacements.TestAdapter);
        }

        private ICoverageProject CreateCoverageProject(Action<Mock<ICoverageProject>> furtherSetup = null, bool includeTestAssembly = true)
        {
            var mockSettings = new Mock<IAppOptions>();
            mockSettings.Setup(settings => settings.IncludeTestAssembly).Returns(includeTestAssembly);
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(new List<IReferencedProject>());
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(new List<IReferencedProject>());
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns("");
            mockCoverageProject.Setup(cp => cp.Settings).Returns(mockSettings.Object);
            furtherSetup?.Invoke(mockCoverageProject);
            return mockCoverageProject.Object;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Set_Enabled_From_The_CoverageProject_Settings(bool enabled)
        {
            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings.Enabled).Returns(enabled));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            Assert.AreEqual(enabled.ToString(), replacements.Enabled);
        }

        [Test]
        public void Should_Set_The_ResultsDirectory_To_The_Project_CoverageOutputFolder()
        {
            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.CoverageOutputFolder).Returns("CoverageOutputFolder"));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            Assert.AreEqual("CoverageOutputFolder", replacements.ResultsDirectory);
        }

        [Test]
        public void Should_Create_Element_Replacements_From_Settings()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                FunctionsExclude = new[] { "FunctionExclude1", "FunctionExclude2" },
                FunctionsInclude = new[] { "FunctionInclude1", "FunctionInclude2" },
                CompanyNamesExclude = new[] { "CompanyNameExclude1", "CompanyNameExclude2" },
                CompanyNamesInclude = new[] { "CompanyNameInclude1", "CompanyNameInclude2" },
                AttributesExclude = new[] { "AttributeExclude1", "AttributeExclude2" },
                AttributesInclude = new[] { "AttributeInclude1", "AttributeInclude2" },
                PublicKeyTokensExclude = new[] { "PublicKeyTokenExclude1", "PublicKeyTokenExclude2" },
                PublicKeyTokensInclude = new[] { "PublicKeyTokenInclude1", "PublicKeyTokenInclude2" },
                SourcesExclude = new[] { "SourceExclude1", "SourceExclude2" },
                SourcesInclude = new[] { "SourceInclude1", "SourceInclude2" },
                IncludeTestAssembly = true,
            };

            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.FunctionsExclude, "Function", msCodeCoverageOptions.FunctionsExclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.FunctionsInclude, "Function", msCodeCoverageOptions.FunctionsInclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.CompanyNamesExclude, "CompanyName", msCodeCoverageOptions.CompanyNamesExclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.CompanyNamesInclude, "CompanyName", msCodeCoverageOptions.CompanyNamesInclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.AttributesExclude, "Attribute", msCodeCoverageOptions.AttributesExclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.AttributesInclude, "Attribute", msCodeCoverageOptions.AttributesInclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.PublicKeyTokensExclude, "PublicKeyToken", msCodeCoverageOptions.PublicKeyTokensExclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.PublicKeyTokensInclude, "PublicKeyToken", msCodeCoverageOptions.PublicKeyTokensInclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.SourcesExclude, "Source", msCodeCoverageOptions.SourcesExclude);
            IncludeExcludeXmlELementsStringAssertionHelper.Assert(replacements.SourcesInclude, "Source", msCodeCoverageOptions.SourcesInclude);
        }

        [Test]
        public void Should_Create_Distinct_Element_Replacements()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                FunctionsExclude = new[] { "FunctionExclude1", "FunctionExclude1" },
                FunctionsInclude = new[] { "FunctionInclude1", "FunctionInclude1" },
                CompanyNamesExclude = new[] { "CompanyNameExclude1", "CompanyNameExclude1" },
                CompanyNamesInclude = new[] { "CompanyNameInclude1", "CompanyNameInclude1" },
                AttributesExclude = new[] { "AttributeExclude1", "AttributeExclude1" },
                AttributesInclude = new[] { "AttributeInclude1", "AttributeInclude1" },
                PublicKeyTokensExclude = new[] { "PublicKeyTokenExclude1", "PublicKeyTokenExclude1" },
                PublicKeyTokensInclude = new[] { "PublicKeyTokenInclude1", "PublicKeyTokenInclude1" },
                SourcesExclude = new[] { "SourceExclude1", "SourceExclude1" },
                SourcesInclude = new[] { "SourceInclude1", "SourceInclude1" },
                ModulePathsExclude = new[] { "ModulePathExclude1", "ModulePathExclude1" },
                ModulePathsInclude = new[] { "ModulePathInclude1", "ModulePathInclude1" },
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            void AssertReplacement(string replacement, string replacementProperty, bool isInclude)
            {
                var ie = isInclude ? "Include" : "Exclude";
                Assert.AreEqual($"<{replacementProperty}>{replacementProperty}{ie}1</{replacementProperty}>", replacement);
            }

            AssertReplacement(replacements.ModulePathsExclude, "ModulePath", false);

            AssertReplacement(replacements.FunctionsExclude, "Function", false);
            AssertReplacement(replacements.FunctionsInclude, "Function", true);
            AssertReplacement(replacements.CompanyNamesExclude, "CompanyName", false);
            AssertReplacement(replacements.CompanyNamesInclude, "CompanyName", true);
            AssertReplacement(replacements.AttributesExclude, "Attribute", false);
            AssertReplacement(replacements.AttributesInclude, "Attribute", true);
            AssertReplacement(replacements.PublicKeyTokensExclude, "PublicKeyToken", false);
            AssertReplacement(replacements.PublicKeyTokensInclude, "PublicKeyToken", true);
            AssertReplacement(replacements.SourcesExclude, "Source", false);
            AssertReplacement(replacements.SourcesInclude, "Source", true);
        }

        [Test]
        public void Should_Be_Null_TestAdapter_Replacement_When_Null()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            Assert.That(replacements.TestAdapter, Is.Null);
        }

        [Test]
        public void Should_Have_ModulePathsExclude_Replacements_From_ExcludedReferencedProjects_Settings_And_Excluded_Test_Assembly()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                ModulePathsExclude = new[] { "FromSettings" },
                IncludeTestAssembly = false
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.ExcludedReferencedProjects).Returns(new List<IReferencedProject>
                {
                    new ReferencedProject("","ModuleName",true)
                });
                mock.Setup(cp => cp.TestDllFile).Returns(@"Path\To\Test.dll");
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsExclude, "ModulePath",
                new[] { MsCodeCoverageRegex.RegexModuleName("ModuleName", true), MsCodeCoverageRegex.RegexEscapePath(@"Path\To\Test.dll"), "FromSettings" });
        }

        [Test]
        public void Should_Have_ModulePathsInclude_Replacements_From_IncludedReferencedProjects_Settings_IncludedTestAssembly_False()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                ModulePathsInclude = new[] { "FromSettings" },
                IncludeTestAssembly = false
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.IncludedReferencedProjects).Returns(new List<IReferencedProject>
                {
                    new ReferencedProject("", "ModuleNameDll", true),
                    new ReferencedProject("", "ModuleNameExe", false)
                });
                mock.Setup(cp => cp.TestDllFile).Returns(@"Path\To\Test.dll");
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                new[] {
                    MsCodeCoverageRegex.RegexModuleName("ModuleNameDll", true),
                    MsCodeCoverageRegex.RegexModuleName("ModuleNameExe", false),
                    "FromSettings" });
        }

        [Test]
        public void Should_Not_Have_ModulePathsInclude_Replacements_When_IncludeTestAssembly_True_And_No_Other_Includes()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.TestDllFile).Returns(@"Path\To\Test.dll");
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                Enumerable.Empty<string>());
        }

        [Test]
        public void Should_Have_ModulePathsInclude_Replacements_With_Test_Assembly_When_IncludeTestAssembly_True_And_Other_Includes()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                ModulePathsInclude = new[] { "SettingsInclude" },
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.TestDllFile).Returns(@"Path\To\Test.dll");
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);

            IncludeExcludeXmlELementsStringAssertionHelper.Assert(
                replacements.ModulePathsInclude,
                "ModulePath",
                new[] { "SettingsInclude", MsCodeCoverageRegex.RegexEscapePath(@"Path\To\Test.dll") });
        }

    }

    internal static class IncludeExcludeXmlELementsStringAssertionHelper {
        public static void Assert(string xmlElements, string expectedElementName, IEnumerable<string> expectedContents)
        {
            var elements = XElement.Parse($"<root>{xmlElements}</root>").Elements();
            NUnit.Framework.Assert.That(elements.All(el => el.Name == expectedElementName));
            NUnit.Framework.Assert.That(elements.Select(el => el.Value), Is.EquivalentTo(expectedContents));
        }
    }

    [ExcludeFromCodeCoverage]
    internal class TestCoverageProjectOptions : IAppOptions
    {
        public string[] Exclude { get; set; }

        public string[] ExcludeByAttribute { get; set; }

        public string[] ExcludeByFile { get; set; }

        public string[] Include { get; set; }

        public bool RunInParallel { get; set; }

        public int RunWhenTestsExceed { get; set; }

        public bool RunWhenTestsFail { get; set; }

        public bool RunSettingsOnly { get; set; }

        public bool CoverletConsoleGlobal { get; set; }

        public string CoverletConsoleCustomPath { get; set; }

        public bool CoverletConsoleLocal { get; set; }

        public string CoverletCollectorDirectoryPath { get; set; }

        public string OpenCoverCustomPath { get; set; }

        public string FCCSolutionOutputDirectoryName { get; set; }

        public int ThresholdForCyclomaticComplexity { get; set; }

        public int ThresholdForNPathComplexity { get; set; }

        public int ThresholdForCrapScore { get; set; }

        public bool CoverageColoursFromFontsAndColours { get; set; }

        public bool StickyCoverageTable { get; set; }

        public bool NamespacedClasses { get; set; }

        public bool HideFullyCovered { get; set; }

        public bool AdjacentBuildOutput { get; set; }

        public FineCodeCoverage.Options.RunMsCodeCoverage RunMsCodeCoverage { get; set; }
        public string[] ModulePathsExclude { get; set; }
        public string[] ModulePathsInclude { get; set; }
        public string[] CompanyNamesExclude { get; set; }
        public string[] CompanyNamesInclude { get; set; }
        public string[] PublicKeyTokensExclude { get; set; }
        public string[] PublicKeyTokensInclude { get; set; }
        public string[] SourcesExclude { get; set; }
        public string[] SourcesInclude { get; set; }
        public string[] AttributesExclude { get; set; }
        public string[] AttributesInclude { get; set; }
        public string[] FunctionsInclude { get; set; }
        public string[] FunctionsExclude { get; set; }

        public bool Enabled { get; set; }

        public bool IncludeTestAssembly { get; set; }

        public bool IncludeReferencedProjects { get; set; }

        public string ToolsDirectory { get; set; }
        public bool ShowCoverageInOverviewMargin { get; set; }
        public bool ShowCoveredInOverviewMargin { get; set; }
        public bool ShowUncoveredInOverviewMargin { get; set; }
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }
        public bool ShowDirtyInOverviewMargin { get; set; }
        public bool ShowNewInOverviewMargin { get; set; }
        public bool ShowToolWindowToolbar { get; set; }
        public bool Hide0Coverable { get; set; }
        public bool Hide0Coverage { get; set; }
        public string[] ExcludeAssemblies { get; set; }
        public string[] IncludeAssemblies { get; set; }
        public bool DisabledNoCoverage { get; set; }
        public NamespaceQualification NamespaceQualification { get; set; }
        public OpenCoverRegister OpenCoverRegister { get; set; }
        public string OpenCoverTarget { get; set; }
        public string OpenCoverTargetArgs { get; set; }
        public bool ShowCoverageInGlyphMargin { get; set; }
        public bool ShowCoveredInGlyphMargin { get; set; }
        public bool ShowUncoveredInGlyphMargin { get; set; }
        public bool ShowPartiallyCoveredInGlyphMargin {get; set; }
        public bool ShowDirtyInGlyphMargin { get; set; }
        public bool ShowNewInGlyphMargin { get; set ; }
        public bool ShowLineCoverageHighlighting { get; set; }
        public bool ShowLineCoveredHighlighting { get; set; }
        public bool ShowLineUncoveredHighlighting { get; set; }
        public bool ShowLinePartiallyCoveredHighlighting { get; set; }
        public bool ShowLineDirtyHighlighting { get; set; }
        public bool ShowLineNewHighlighting { get; set; }
        public bool ShowEditorCoverage { get; set; }
        public bool UseEnterpriseFontsAndColors { get; set; }
        public EditorCoverageColouringMode EditorCoverageColouringMode { get; set; }
        public bool ShowNotIncludedInOverviewMargin { get; set; }
        public bool ShowNotIncludedInGlyphMargin { get; set; }
        public bool ShowLineNotIncludedHighlighting { get; set; }
        public bool BlazorCoverageLinesFromGeneratedSource { get; set; }
    }
}
