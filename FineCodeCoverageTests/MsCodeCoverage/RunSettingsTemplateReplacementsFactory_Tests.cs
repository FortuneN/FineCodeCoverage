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
            public List<string> ExcludedReferencedProjects { get; set; }
            public List<string> IncludedReferencedProjects { get; set; }
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
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
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
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = outputFolder2,
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
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
                    IncludeTestAssembly = true,

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
                        Settings = CreateSettings("1"),
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = CreateSettings("2"),
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
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

            void AssertReplacement(string replacement, string replacementProperty, bool isInclude)
            {
                var ie = isInclude ? "Include" : "Exclude";
                Assert.AreEqual($"<{replacementProperty}>{replacementProperty}{ie}1</{replacementProperty}><{replacementProperty}>{replacementProperty}{ie}2</{replacementProperty}>", replacement);
            }

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
                            ModulePathsExclude = new string[]{ "ModulePathExclude"}
                        },
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        TestDllFile = @"Some\Path1"
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = includeTestAssembly2},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                        TestDllFile = @"Some\Path2"
                    }
                },
            };

            var testDlls = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value.TestDllFile).ToList();
            string GetModulePathExcludeWhenExcludingTestAssembly(bool first)
            {
                var regexed = MsCodeCoverageRegex.RegexEscapePath(testDlls[first ? 0 : 1]);
                return ModulePathElement(regexed);
            }

            var expectedModulePathExcludes1 = !includeTestAssembly1 ? GetModulePathExcludeWhenExcludingTestAssembly(true) : "";
            var expectedModulePathExcludes2 = !includeTestAssembly2 ? GetModulePathExcludeWhenExcludingTestAssembly(false) : "";
            var expectedModulePathExcludes = expectedModulePathExcludes1 + expectedModulePathExcludes2 + ModulePathElement("ModulePathExclude");

            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);
            Assert.AreEqual(expectedModulePathExcludes, replacements.ModulePathsExclude);
        }

        [Test]
        public void Should_Add_Regexed_IncludedExcluded_Referenced_Projects_To_ModulePaths()
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
                            IncludeTestAssembly = true,
                            ModulePathsExclude = new string[]{ "ModulePathExclude"},
                            ModulePathsInclude = new string[]{ "ModulePathInclude"}
                        },
                        ExcludedReferencedProjects = new List<string>{ "ExcludedReferenced1"},
                        IncludedReferencedProjects = new List<string>{ "IncludedReferenced1" },
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>{ "ExcludedReferenced2"},
                        IncludedReferencedProjects = new List<string>{ "IncludedReferenced2" },
                    }
                },
            };

            var projectDetails = userRunSettingsProjectDetailsLookup.Select(kvp => kvp.Value).ToList();
            var allExcludedReferencesProjects = projectDetails.SelectMany(pd => pd.ExcludedReferencedProjects);
            var allIncludedReferencesProjects = projectDetails.SelectMany(pd => pd.IncludedReferencedProjects);

            string GetExpectedExcludedOrIncludedEscaped(IEnumerable<string> excludedOrIncludedReferenced)
            {
                return string.Join("", excludedOrIncludedReferenced.Select(referenced => ModulePathElement(MsCodeCoverageRegex.RegexModuleName(referenced))));
            }

            var expectedModulePathExcludes = GetExpectedExcludedOrIncludedEscaped(allExcludedReferencesProjects) + ModulePathElement("ModulePathExclude");
            var expectedModulePathIncludes = GetExpectedExcludedOrIncludedEscaped(allIncludedReferencesProjects) + ModulePathElement("ModulePathInclude");

            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);
            Assert.AreEqual(expectedModulePathExcludes, replacements.ModulePathsExclude);
            Assert.AreEqual(expectedModulePathIncludes, replacements.ModulePathsInclude);
        }

        [Test]
        public void Should_Be_Empty_String_Replacement_When_Null()
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
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{ IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                }
            };
            var replacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, null);
            ReplacementsAssertions.AssertAllEmpty(replacements);
        }

        [TestCase(true,true,"true")]
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
                        Settings = new TestMsCodeCoverageOptions{ Enabled = project1Enabled, IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                },
                {
                    "Source2",
                    new TestUserRunSettingsProjectDetails
                    {
                        CoverageOutputFolder = "",
                        Settings = new TestMsCodeCoverageOptions{ Enabled = project2Enabled,  IncludeTestAssembly = true},
                        ExcludedReferencedProjects = new List<string>(),
                        IncludedReferencedProjects = new List<string>(),
                    }
                }
            };

            var runSettingsTemplateReplacements = runSettingsTemplateReplacementsFactory.Create(testContainers, userRunSettingsProjectDetailsLookup, "");

            Assert.That(runSettingsTemplateReplacements.Enabled, Is.EqualTo(expectedEnabled));
        }

        private string ModulePathElement(string value)
        {
            return $"<ModulePath>{value}</ModulePath>";
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

        private ICoverageProject CreateCoverageProject(Action<Mock<ICoverageProject>> furtherSetup = null)
        {
            var mockSettings = new Mock<IAppOptions>();
            mockSettings.Setup(settings => settings.IncludeTestAssembly).Returns(true);
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ExcludedReferencedProjects).Returns(new List<string>());
            mockCoverageProject.Setup(cp => cp.IncludedReferencedProjects).Returns(new List<string>());
            mockCoverageProject.Setup(cp  => cp.Settings).Returns(mockSettings.Object);
            furtherSetup?.Invoke(mockCoverageProject);
            return mockCoverageProject.Object;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Set_Enabled_From_The_CoverageProject_Settings(bool enabled)
        {
            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings.Enabled).Returns(enabled));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject,null);
            Assert.AreEqual(enabled.ToString(), replacements.Enabled);
        }

        [Test]
        public void Should_Set_The_ResultsDirectory_To_The_Project_CoverageOutputFolder()
        {
            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.CoverageOutputFolder).Returns("CoverageOutputFolder"));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject,null);
            Assert.AreEqual("CoverageOutputFolder", replacements.ResultsDirectory);
        }

        [Test]
        public void Should_Create_Element_Replacements()
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
                ModulePathsExclude = new[] { "ModulePathExclude1","ModulePathExclude2" },
                ModulePathsInclude = new[] { "ModulePathInclude1", "ModulePathInclude2" },
                IncludeTestAssembly = true,
            };

            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);


            void AssertReplacement(string replacement, string replacementProperty, bool isInclude)
            {
                var ie = isInclude ? "Include" : "Exclude";
                Assert.AreEqual($"<{replacementProperty}>{replacementProperty}{ie}1</{replacementProperty}><{replacementProperty}>{replacementProperty}{ie}2</{replacementProperty}>", replacement);
            }

            AssertReplacement(replacements.ModulePathsExclude, "ModulePath", false);
            AssertReplacement(replacements.ModulePathsInclude, "ModulePath", true);
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
            AssertReplacement(replacements.ModulePathsInclude, "ModulePath", true);

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
        public void Should_Be_Empty_String_Replacement_When_Null()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock => mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions));
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            

            ReplacementsAssertions.AssertAllEmpty(replacements);
        }

        [Test]
        public void Should_Have_ModulePathsExclude_Replacements_From_ExcludedReferencedProjects_Settings_And_Excluded_Test_Assembly()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                ModulePathsExclude = new[] { "FromSettings"},
                IncludeTestAssembly = false
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.ExcludedReferencedProjects).Returns(new List<string>
                {
                    "ModuleName"
                });
                mock.Setup(cp => cp.TestDllFile).Returns(@"Path\To\Test.dll");
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            var expectedModulePathsExclude = $"{ModulePathElement(MsCodeCoverageRegex.RegexModuleName("ModuleName"))}{ModulePathElement(MsCodeCoverageRegex.RegexEscapePath(@"Path\To\Test.dll"))}{ModulePathElement("FromSettings")}";
            Assert.AreEqual(expectedModulePathsExclude, replacements.ModulePathsExclude);
        }

        [Test]
        public void Should_Have_ModulePathsInclude_Replacements_From_IncludedReferencedProjects_And_Settings()
        {
            var msCodeCoverageOptions = new TestCoverageProjectOptions
            {
                ModulePathsInclude = new[] { "FromSettings" },
                IncludeTestAssembly = true
            };

            var coverageProject = CreateCoverageProject(mock =>
            {
                mock.Setup(cp => cp.Settings).Returns(msCodeCoverageOptions);
                mock.Setup(cp => cp.IncludedReferencedProjects).Returns(new List<string>
                {
                    "ModuleName"
                });
            });

            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, null);
            var expectedModulePathsInclude = $"{ModulePathElement(MsCodeCoverageRegex.RegexModuleName("ModuleName"))}{ModulePathElement("FromSettings")}";
            Assert.AreEqual(expectedModulePathsInclude, replacements.ModulePathsInclude);
        }

        private static string ModulePathElement(string value)
        {
            return $"<ModulePath>{value}</ModulePath>";
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
    }
}
