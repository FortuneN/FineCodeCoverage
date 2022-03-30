using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.Test_helpers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Test
{
    public class CoverageProject_Settings_Tests
    {
        //[Test]
        public void Should_Get_Settings_From_CoverageProjectSettingsManager()
        {

        }

    }

    public class FCCSettingsFilesProvider_Tests
    {
        [Test]
        public void Should_Return_All_FCC_Options_In_Project_Folder_And_Ascendants_Top_Level_First()
        {
            var fccOptionsElements = Provide("<Root1></Root1>", "<Root2></Root2>");
            Assert.True(fccOptionsElements.Count == 2);
            Assert.True(fccOptionsElements[0].Name == "Root2");
            Assert.True(fccOptionsElements[1].Name == "Root1");
        }

        [Test]
        public void Should_Stop_At_TopLevel()
        {
            var fccOptionsElements = Provide("<Root1 topLevel='true'></Root1>", "<Root2></Root2>");
            Assert.True(fccOptionsElements.Count == 1);
            Assert.True(fccOptionsElements[0].Name == "Root1");
        }

        [Test]
        public void Should_Ignore_Exceptions()
        {
            var fccOptionsElements = Provide("<Bad", "<Root2></Root2>");
            Assert.True(fccOptionsElements.Count == 1);
            Assert.True(fccOptionsElements[0].Name == "Root2");
        }

        private List<XElement> Provide(string projectDirectoryFCCOptions, string solutionParentDirectoryFCCOptions)
        {
            var projectPath = "projectPath";
            var mockFileUtil = new Mock<IFileUtil>();
            var projectDirectoryFCCOptionsPath = Path.Combine(projectPath, FCCSettingsFilesProvider.fccOptionsFileName);
            mockFileUtil.Setup(fileUtil => fileUtil.Exists(projectDirectoryFCCOptionsPath)).Returns(true);
            mockFileUtil.Setup(fileUtil => fileUtil.ReadAllText(projectDirectoryFCCOptionsPath)).Returns(projectDirectoryFCCOptions);
            
            var solutionPath = "Solution";
            var solutionDirectoryFCCOptionsPath = Path.Combine(solutionPath, FCCSettingsFilesProvider.fccOptionsFileName);
            mockFileUtil.Setup(fileUtil => fileUtil.DirectoryParentPath(projectPath)).Returns(solutionPath);

            // will want a gap where it does not exist
            mockFileUtil.Setup(fileUtil => fileUtil.Exists(solutionDirectoryFCCOptionsPath)).Returns(false);

            var solutionParentPath = "SolutionParent";
            var solutionParentDirectoryFCCOptionsPath = Path.Combine(solutionParentPath, FCCSettingsFilesProvider.fccOptionsFileName);
            mockFileUtil.Setup(fileUtil => fileUtil.DirectoryParentPath(solutionPath)).Returns(solutionParentPath);

            mockFileUtil.Setup(fileUtil => fileUtil.Exists(solutionParentDirectoryFCCOptionsPath)).Returns(true);
            mockFileUtil.Setup(fileUtil => fileUtil.ReadAllText(solutionParentDirectoryFCCOptionsPath)).Returns(solutionParentDirectoryFCCOptions);
            mockFileUtil.Setup(fileUtil => fileUtil.DirectoryParentPath(solutionParentPath)).Returns((string)null);


            var fccOptionsProvider = new FCCSettingsFilesProvider(mockFileUtil.Object);
            return fccOptionsProvider.Provide(projectPath);
        }
    }

    public class CoverageProjectSettingsProvider_Tests
    {
        [Test]
        public async Task Should_Return_The_FineCodeCoverage_Labelled_PropertyGroup()
        {
            var coverageProjectSettingsProvider = new CoverageProjectSettingsProvider(null);
            var mockCoverageProject = new Mock<ICoverageProject>();
            var fccLabelledPropertyGroup = @"
    <PropertyGroup Label='FineCodeCoverage'>
        <Setting1/>
    </PropertyGroup>

";
            var projectFileXElement = XElement.Parse($@"
<Project>
    {fccLabelledPropertyGroup}
</Project>
");
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(projectFileXElement);
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsProvider.ProvideAsync(coverageProject);
            XmlAssert.NoXmlDifferences(coverageProjectSettings.ToString(), fccLabelledPropertyGroup);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Return_Using_VsBuild_When_No_Labelled_PropertyGroup(bool returnNull)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            var coverageProjectGuid = Guid.NewGuid();
            mockCoverageProject.Setup(cp => cp.Id).Returns(coverageProjectGuid);
            var fccLabelledPropertyGroup = @"
    <PropertyGroup Label='NotFineCodeCoverage'>
    </PropertyGroup>

";
            var projectFileXElement = XElement.Parse($@"
<Project>
    {fccLabelledPropertyGroup}
</Project>
");
            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(projectFileXElement);

            var mockVsBuildFCCSettingsProvider = new Mock<IVsBuildFCCSettingsProvider>();
            var settingsElementFromVsBuildFCCSettingsProvider = returnNull ? null : new XElement("Root");
            mockVsBuildFCCSettingsProvider.Setup(
                vsBuildFCCSettingsProvider =>
                vsBuildFCCSettingsProvider.GetSettingsAsync(coverageProjectGuid)
            ).ReturnsAsync(settingsElementFromVsBuildFCCSettingsProvider);

            var coverageProjectSettingsProvider = new CoverageProjectSettingsProvider(mockVsBuildFCCSettingsProvider.Object);
            
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsProvider.ProvideAsync(coverageProject);

            Assert.AreSame(settingsElementFromVsBuildFCCSettingsProvider, coverageProjectSettings);
        }
    }

    public class SettingsMerger_Tests
    {
        [Test]
        public void Should_Use_Global_Settings_If_No_Project_Level_Or_FCC_Settings_Files()
        {
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            var appOptions = mockAppOptions.Object;

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(appOptions, new List<XElement>(), null);
            
            Assert.AreSame(appOptions, mergedSettings);
        }

        [Test]
        public void Should_Overwrite_GlobalOptions_Bool_Properties_From_Settings_File()
        {
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            mockAppOptions.SetupSet(o => o.IncludeReferencedProjects = true);
            var appOptions = mockAppOptions.Object;

            var settingsMerger = new SettingsMerger(null);
            var settingsFileElement = CreateIncludeReferencedProjectsElement(true);
            var mergedSettings = settingsMerger.Merge(appOptions, new List<XElement> { settingsFileElement}, null);

            Assert.AreSame(appOptions, mergedSettings);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Overwrite_GlobalOptions_Bool_Properties_From_Settings_File_In_Order(bool last)
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var settingsMerger = new SettingsMerger(null);
            var settingsFileElementTop = CreateIncludeReferencedProjectsElement(!last);
            var settingsFileElementLast = CreateIncludeReferencedProjectsElement(last);
            var mergedSettings = settingsMerger.Merge(
                appOptions, 
                new List<XElement> { settingsFileElementTop, settingsFileElementLast}, 
                null);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(last, appOptions.IncludeReferencedProjects);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Overwrite_GlobalOptions_Bool_Properties_From_Project(bool last)
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var settingsMerger = new SettingsMerger(null);
            var settingsFileElement = CreateIncludeReferencedProjectsElement(!last);
            var projectElement = CreateIncludeReferencedProjectsElement(last);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { settingsFileElement },
                projectElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(last, appOptions.IncludeReferencedProjects);
        }

        [Test]
        public void Should_Overwrite_Int_Properties()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var intElement = XElement.Parse($@"
<Root>
    <ThresholdForCyclomaticComplexity>123</ThresholdForCyclomaticComplexity>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> {},
                intElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(123, appOptions.ThresholdForCyclomaticComplexity);
        }

        [Test]
        public void Should_Overwrite_String_Properties()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var stringElement = XElement.Parse($@"
<Root>
    <ToolsDirectory>ToolsDirectory</ToolsDirectory>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual("ToolsDirectory", appOptions.ToolsDirectory);
        }

        [Test]
        public void Should_Overwrite_String_Array_By_Default()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var stringArrayElement = XElement.Parse($@"
<Root>
  <Exclude>
    1
    2
  </Exclude>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2"}, appOptions.Exclude);
        }

        [Test]
        public void Should_Merge_String_Array_If_DefaultMerge()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root defaultMerge='true'>
  <Exclude>
    1
    2
  </Exclude>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "global","1", "2" }, appOptions.Exclude);
        }

        [Test]
        public void Should_Merge_If_Property_Element_Merge()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root>
  <Exclude merge='true'>
    1
    2
  </Exclude>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "global", "1", "2" }, appOptions.Exclude);
        }

        [Test]
        public void Should_Not_Throw_If_Merge_Current_Null_String_Array_Type()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = null;
            var stringArrayElement = XElement.Parse($@"
<Root>
  <Exclude merge='true'>
    1
    2
  </Exclude>
</Root>
");

            var settingsMerger = new SettingsMerger(null);
            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2" }, appOptions.Exclude);
        }

        [TestCaseSource(nameof(XmlConversionCases))]
        public void Should_Convert_Xml_Value_Correctly(string propertyElement,string propertyName,object expectedConversion, bool expectedException)
        {
            var settingsMerger = new SettingsMerger(new Mock<ILogger>().Object);
            var settingsElement = XElement.Parse($"<Root>{propertyElement}</Root>");
            var property = typeof(IAppOptions).GetPublicProperties().First(p => p.Name == propertyName);
            if (expectedException)
            {
                Assert.Throws<Exception>(() =>
                {
                    settingsMerger.GetValueFromXml(settingsElement, property);
                });
            }
            else
            {
                var value = settingsMerger.GetValueFromXml(settingsElement, property);
                Assert.AreEqual(expectedConversion, value);
            }
        }

        static object[] XmlConversionCases()
        {
            string CreateElement(string elementName, string value)
            {
                return $"<{elementName}>{value}</{elementName}>";
            }
            var hideFullyCovered = nameof(IAppOptions.HideFullyCovered); // bool
            var thresholdForCrapScore = nameof(IAppOptions.ThresholdForCrapScore); // int
            var coverletConsoleCustomPath = nameof(IAppOptions.CoverletConsoleCustomPath); // string
            var exclude = nameof(IAppOptions.Exclude); // string[]
            var noConversion = nameof(IAppOptions.RunMsCodeCoverage); // no conversion
            var boolArray = @"
                true
                false
            ";
            var stringArray = @"
                1
                2
            ";
            var cases = new object[]
            {
                // boolean
                new object[]{ CreateElement(hideFullyCovered, "true"),hideFullyCovered,true, false },
                new object[]{ CreateElement(hideFullyCovered, "false"), hideFullyCovered, false, false },
                new object[]{ CreateElement(hideFullyCovered, "bad"), hideFullyCovered, null, false },
                new object[]{ CreateElement(hideFullyCovered, ""), hideFullyCovered, null, false },
                new object[]{ CreateElement(hideFullyCovered, boolArray), hideFullyCovered, true, false },

                // int
                new object[]{ CreateElement(thresholdForCrapScore, "1"), thresholdForCrapScore, 1, false },
                new object[]{ CreateElement(thresholdForCrapScore, "bad"), thresholdForCrapScore, null, false },
                new object[]{ CreateElement(thresholdForCrapScore, ""), thresholdForCrapScore, null, false },

                // string
                new object[]{ CreateElement(coverletConsoleCustomPath, "1"), coverletConsoleCustomPath, "1", false },
                // breaking change ( previous ignored )
                new object[]{ CreateElement(coverletConsoleCustomPath, ""), coverletConsoleCustomPath, "", false },

                // string[] 
                new object[]{ CreateElement(exclude, stringArray), exclude, new string[] { "1","2"}, false },
                new object[]{ CreateElement(exclude, ""), exclude, new string[] {}, false },

                // null for no property element
                new object[]{ CreateElement(exclude, "true"), hideFullyCovered, null, false},

                //exception for no type conversion
                new object[]{ CreateElement(noConversion, "No"), noConversion, null, true }
                
            };

            return cases;
        }


        private XElement CreateIncludeReferencedProjectsElement(bool include)
        {
            return XElement.Parse($@"
<Root>
    <IncludeReferencedProjects>{include}</IncludeReferencedProjects>
</Root>
");
        }

        //        [Test]
        //        public async Task Should_Prefer_ProjectLevel_From_FCC_Labelled_PropertyGroup_Over_Global()
        //        {
        //            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
        //            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
        //            mockAppOptions.SetupSet(o => o.ThresholdForCrapScore = 123); // int type
        //            mockAppOptions.SetupSet(o => o.CoverletCollectorDirectoryPath = "CoverletCollectorDirectoryPath"); // string type
        //            
        //            mockAppOptions.SetupSet(o => o.Exclude = new string[] { "1","2"}); // string array
        //            var appOptions = mockAppOptions.Object;
        //            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(appOptions);

        //            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
        //                mockAppOptionsProvider.Object,
        //                // does not use if has FineCodeCoverage PropertyGroup with label
        //                new Mock<IVsBuildFCCSettingsProvider>(MockBehavior.Strict).Object,
        //                new Mock<IFCCSettingsFilesProvider>().Object,
        //                new Mock<ISettingsMerger>().Object
        //            );

        //            var mockCoverageProject = new Mock<ICoverageProject>();
        //            var projectFileElement = XElement.Parse(@"
        //<Project>

        //<PropertyGroup Label='FineCodeCoverage'>
        //    <ThresholdForCrapScore>123</ThresholdForCrapScore>
        //    <CoverletCollectorDirectoryPath>CoverletCollectorDirectoryPath</CoverletCollectorDirectoryPath>
        //    <IncludeReferencedProjects>true</IncludeReferencedProjects>
        //    <Exclude>
        //        1
        //        2
        //    </Exclude>
        //</PropertyGroup>
        //</Project>
        //");
        //            mockCoverageProject.Setup(cp => cp.ProjectFileXElement).Returns(projectFileElement);
        //            var coverageProject = mockCoverageProject.Object;
        //            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
        //            Assert.AreSame(appOptions, coverageProjectSettings);
        //            mockAppOptions.VerifyAll();
        //        }
    }

    public class CoverageProjectSettingsManager_Tests
    {
        [Test]
        public async Task Should_Provide_The_Merged_Result_Using_Global_Options()
        {
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>();
            var globalOptions = mockAppOptions.Object;
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(globalOptions);

            var mockSettingsMerger = new Mock<ISettingsMerger>();
            var mergedSettings = new Mock<IAppOptions>().Object;
            mockSettingsMerger.Setup(settingsMerger =>
                settingsMerger.Merge(globalOptions, It.IsAny<List<XElement>>(), It.IsAny<XElement>())
            ).Returns(mergedSettings);

            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                mockAppOptionsProvider.Object,
                new Mock<ICoverageProjectSettingsProvider>().Object,
                new Mock<IFCCSettingsFilesProvider>().Object,
                mockSettingsMerger.Object
            );

            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(
                new Mock<ICoverageProject>().Object
            );
            Assert.AreSame(mergedSettings, coverageProjectSettings);
        }

        [Test]
        public async Task Should_Provide_The_Merged_Result_Using_FCC_Settings_Files()
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.ProjectFile).Returns("SomeProject/SomeProject.csproj");

            var mockFCCSettingsFilesProvider = new Mock<IFCCSettingsFilesProvider>();
            var settingsFileElements = new List<XElement>();
            mockFCCSettingsFilesProvider.Setup(
                fccSettingsFilesProvider => fccSettingsFilesProvider.Provide("SomeProject")
            ).Returns(settingsFileElements);

            var mockSettingsMerger = new Mock<ISettingsMerger>();
            var mergedSettings = new Mock<IAppOptions>().Object;
            mockSettingsMerger.Setup(settingsMerger =>
                settingsMerger.Merge(It.IsAny<IAppOptions>(), settingsFileElements, It.IsAny<XElement>())
            ).Returns(mergedSettings);

            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                new Mock<IAppOptionsProvider>().Object,
                new Mock<ICoverageProjectSettingsProvider>().Object,
                mockFCCSettingsFilesProvider.Object,
                mockSettingsMerger.Object
            );

            
            var coverageProject = mockCoverageProject.Object;
            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            Assert.AreSame(mergedSettings, coverageProjectSettings);
        }

        [Test]
        public async Task Should_Provide_The_Merged_Result_Using_Project_Settings()
        {
            var coverageProject = new Mock<ICoverageProject>().Object;

            var coverageProjectSettingsElement = new XElement("Root");
            var mockCoverageProjectSettingsProvider = new Mock<ICoverageProjectSettingsProvider>();
            mockCoverageProjectSettingsProvider.Setup(
                coverageProjectSettingsProvider => coverageProjectSettingsProvider.ProvideAsync(coverageProject)
            ).ReturnsAsync(coverageProjectSettingsElement);

            var mockSettingsMerger = new Mock<ISettingsMerger>();
            var mergedSettings = new Mock<IAppOptions>().Object;
            mockSettingsMerger.Setup(settingsMerger =>
                settingsMerger.Merge(It.IsAny<IAppOptions>(), It.IsAny<List<XElement>>(), coverageProjectSettingsElement)
            ).Returns(mergedSettings);

            var coverageProjectSettingsManager = new CoverageProjectSettingsManager(
                new Mock<IAppOptionsProvider>().Object,
                mockCoverageProjectSettingsProvider.Object,
                new Mock<IFCCSettingsFilesProvider>().Object,
                mockSettingsMerger.Object
            );


            
            var coverageProjectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            Assert.AreSame(mergedSettings, coverageProjectSettings);
        }
    }
}