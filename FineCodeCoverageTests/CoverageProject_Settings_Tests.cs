using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using FineCodeCoverage.Output;
using FineCodeCoverageTests.TestHelpers;
using Moq;
using NUnit.Framework;
using StructureMap.AutoMocking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverageTests
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
        public async Task Should_Return_The_FineCodeCoverage_Labelled_PropertyGroup_Async()
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
        public async Task Should_Return_Using_VsBuild_When_No_Labelled_PropertyGroup_Async(bool returnNull)
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
        private AutoMoqer mocker;
        private SettingsMerger settingsMerger;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            settingsMerger = mocker.Create<SettingsMerger>();
        }
        [Test]
        public void Should_Use_Global_Settings_If_No_Project_Level_Or_FCC_Settings_Files()
        {
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            var appOptions = mockAppOptions.Object;

            var mergedSettings = settingsMerger.Merge(appOptions, new List<XElement>(), null);
            
            Assert.AreSame(appOptions, mergedSettings);
        }

        [Test]
        public void Should_Overwrite_GlobalOptions_Bool_Properties_From_Settings_File()
        {
            var mockAppOptions = new Mock<IAppOptions>(MockBehavior.Strict);
            mockAppOptions.SetupSet(o => o.IncludeReferencedProjects = true);
            var appOptions = mockAppOptions.Object;

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

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> {},
                intElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(123, appOptions.ThresholdForCyclomaticComplexity);
        }

        [Test]
        public void Should_Overwrite_Enum_Properties()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            var enumElement = XElement.Parse($@"
<Root>
    <RunMsCodeCoverage>IfInRunSettings</RunMsCodeCoverage>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                enumElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(RunMsCodeCoverage.IfInRunSettings, appOptions.RunMsCodeCoverage);
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
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root>
  <Exclude>
    1
    2
  </Exclude>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2"}, appOptions.Exclude);
        }

        [Test]
        public void Should_Overwrite_String_Array_DefaultMerge_False()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root defaultMerge='false'>
  <Exclude>
    1
    2
  </Exclude>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2" }, appOptions.Exclude);
        }

        [Test]
        public void Should_Overwrite_String_Array_DefaultMerge_True_Property_Merge_false()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root defaultMerge='true'>
  <Exclude merge='false'>
    1
    2
  </Exclude>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2" }, appOptions.Exclude);
        }

        [Test]
        public void Should_Overwrite_String_Array_DefaultMerge_Not_Bool()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "global" };
            var stringArrayElement = XElement.Parse($@"
<Root defaultMerge='xxx'>
  <Exclude>
    1
    2
  </Exclude>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "1", "2" }, appOptions.Exclude);
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
<Root defaultMerge='false'>
  <Exclude merge='true'>
    1
    2
  </Exclude>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                stringArrayElement);

            Assert.AreSame(appOptions, mergedSettings);
            Assert.AreEqual(new string[] { "global", "1", "2" }, appOptions.Exclude);
        }

        [Test]
        public void Should_Log_Failed_To_Get_Setting_From_Project_Settings_Exception_And_Not_Throw()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            var element = XElement.Parse($@"
<Root>
  <OpenCoverRegister>
    DefaultX
  </OpenCoverRegister>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { },
                element);

            var mockLogger = mocker.GetMock<ILogger>();
            mockLogger.Verify(logger => logger.Log("Failed to get 'OpenCoverRegister' setting from project settings", It.IsAny<Exception>()));
            Assert.AreEqual(mergedSettings.OpenCoverRegister, OpenCoverRegister.Default);
        }

        [Test]
        public void Should_Log_Failed_To_Get_Setting_From_Settings_File_Exception_And_Not_Throw()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            var element = XElement.Parse($@"
<Root>
  <OpenCoverRegister>
    DefaultX
  </OpenCoverRegister>
</Root>
");

            var mergedSettings = settingsMerger.Merge(
                appOptions,
                new List<XElement> { element},
                null);

            var mockLogger = mocker.GetMock<ILogger>();
            mockLogger.Verify(logger => logger.Log("Failed to get 'OpenCoverRegister' setting from settings file", It.IsAny<Exception>()));
            Assert.AreEqual(mergedSettings.OpenCoverRegister, OpenCoverRegister.Default);
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
        public void Should_Convert_Xml_Value_Correctly(string propertyElement,string propertyName,object expectedConversion)
        {
            var settingsMerger = new SettingsMerger(new Mock<ILogger>().Object);
            var settingsElement = XElement.Parse($"<Root>{propertyElement}</Root>");
            var property = typeof(IAppOptions).GetPublicProperties().First(p => p.Name == propertyName);
            
            var value = settingsMerger.GetValueFromXml(settingsElement, property);
            Assert.AreEqual(expectedConversion, value);
            
        }

        [Test]
        public void Should_Throw_For_Unsupported_Conversion()
        {
            var settingsElement = XElement.Parse($"<Root><PropertyType/></Root>");
            var unsupported = typeof(PropertyInfo).GetProperty(nameof(PropertyInfo.PropertyType));

            var expectedMessage = $"Unexpected settings type Type for setting PropertyType in settings merger GetValueFromXml";
            Assert.Throws<Exception>(() => settingsMerger.GetValueFromXml(settingsElement, unsupported), expectedMessage);
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
            var enumConversion = nameof(IAppOptions.RunMsCodeCoverage); // enum conversion
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
                new object[]{ CreateElement(hideFullyCovered, "true"),hideFullyCovered,true },
                new object[]{ CreateElement(hideFullyCovered, "false"), hideFullyCovered, false },
                new object[]{ CreateElement(hideFullyCovered, "bad"), hideFullyCovered, null },
                new object[]{ CreateElement(hideFullyCovered, ""), hideFullyCovered, null },
                new object[]{ CreateElement(hideFullyCovered, boolArray), hideFullyCovered, true },

                // int
                new object[]{ CreateElement(thresholdForCrapScore, "1"), thresholdForCrapScore, 1 },
                new object[]{ CreateElement(thresholdForCrapScore, "bad"), thresholdForCrapScore, null },
                new object[]{ CreateElement(thresholdForCrapScore, ""), thresholdForCrapScore, null },

                // string
                new object[]{ CreateElement(coverletConsoleCustomPath, "1"), coverletConsoleCustomPath, "1" },
                // breaking change ( previous ignored )
                new object[]{ CreateElement(coverletConsoleCustomPath, ""), coverletConsoleCustomPath, "" },

                // string[] 
                new object[]{ CreateElement(exclude, stringArray), exclude, new string[] { "1","2"} },
                new object[]{ CreateElement(exclude, ""), exclude, new string[] {} },

                // null for no property element
                new object[]{ CreateElement(exclude, "true"), hideFullyCovered, null},

                //exception for no type conversion
                new object[]{ CreateElement(enumConversion, "No"), enumConversion, RunMsCodeCoverage.No }
                
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
        public async Task Should_Provide_The_Merged_Result_Using_Global_Options_Async()
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
        public async Task Should_Provide_The_Merged_Result_Using_FCC_Settings_Files_Async()
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
        public async Task Should_Provide_The_Merged_Result_Using_Project_Settings_Async()
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

        [Test]
        public async Task Should_Add_Common_Assembly_Excludes_Includes_Ignoring_Whitespace_Async()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;
            appOptions.Exclude = new string[] { "oldexclude" };
            appOptions.Include = new string[] { "oldinclude" };
            appOptions.ModulePathsExclude = new string[] { "msexclude" };
            appOptions.ModulePathsInclude = new string[] { "msinclude" };
            appOptions.ExcludeAssemblies = new string[] { "excludeassembly", " "};
            appOptions.IncludeAssemblies = new string[] { "includeassembly", " "};

            var autoMoqer = new AutoMoqer();
            var coverageProjectSettingsManager = autoMoqer.Create<CoverageProjectSettingsManager>();
            autoMoqer.GetMock<ISettingsMerger>().Setup(settingsMerger => settingsMerger.Merge(
                It.IsAny<IAppOptions>(),
                It.IsAny<List<XElement>>(),
                It.IsAny<XElement>()
                )).Returns(appOptions);

            var settings = await coverageProjectSettingsManager.GetSettingsAsync(new Mock<ICoverageProject>().Object);
            
            Assert.That(settings.Exclude, Is.EquivalentTo(new string[] { "oldexclude", "[excludeassembly]*" }));
            Assert.That(settings.Include, Is.EquivalentTo(new string[] { "oldinclude", "[includeassembly]*" }));
            Assert.That(settings.ModulePathsExclude, Is.EquivalentTo(new string[] { "msexclude", ".*\\excludeassembly.dll$" }));
            Assert.That(settings.ModulePathsInclude, Is.EquivalentTo(new string[] { "msinclude", ".*\\includeassembly.dll$" }));
        }
    }
}