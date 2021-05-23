using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using AutoMoq;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using Org.XmlUnit.Builder;

namespace Test
{
    internal class DataCollectorSettingsBuilder_Tests
    {
        private AutoMoqer mocker;
        private DataCollectorSettingsBuilder dataCollectorSettingsBuilder;
        private string generatedRunSettingsPath;
        private string existingRunSettingsPath;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            dataCollectorSettingsBuilder = mocker.Create<DataCollectorSettingsBuilder>();
            generatedRunSettingsPath = Path.GetTempFileName();
            existingRunSettingsPath = Path.GetTempFileName();
        }

        [TearDown]
        public void DeleteRunSettings()
        {
            File.Delete(generatedRunSettingsPath);
            File.Delete(existingRunSettingsPath);
        }

        private void Initialize(bool runSettingsOnly = false)
        {
            var mockCoverageProjectSettings = new Mock<IAppOptions>();
            mockCoverageProjectSettings.Setup(o => o.RunSettingsOnly).Returns(runSettingsOnly);
            dataCollectorSettingsBuilder.Initialize(mockCoverageProjectSettings.Object, existingRunSettingsPath, generatedRunSettingsPath);
        }

        #region arguments

        [Test]
        public void Should_Safely_Quote_Paths_When_Quote()
        {
            var quoted = dataCollectorSettingsBuilder.Quote(@"C\Some Path");
            Assert.AreEqual(@"""C\Some Path""", quoted);
        }

        [Test]
        public void Should_Set_Blame_Flag_When_WithBlame()
        {
            dataCollectorSettingsBuilder.WithBlame();
            Assert.AreEqual(dataCollectorSettingsBuilder.Blame, "--blame");
        }

        [Test]
        public void Should_Set_NoLogo_Flag_When_WithNoLogo()
        {
            dataCollectorSettingsBuilder.WithNoLogo();
            Assert.AreEqual("--nologo", dataCollectorSettingsBuilder.NoLogo);
        }

        [Test]
        public void Should_Set_Diagnostics_Flag_Quoted_When_WithDiagnostics()
        {
            dataCollectorSettingsBuilder.WithDiagnostics("path");
            Assert.AreEqual(dataCollectorSettingsBuilder.Diagnostics, $"--diag {dataCollectorSettingsBuilder.Quote("path")}");
        }

        [Test]
        public void Should_Set_Results_Directory_Flag_Quoted_When_WithResultsDirectory()
        {
            dataCollectorSettingsBuilder.WithResultsDirectory("path");
            Assert.AreEqual(dataCollectorSettingsBuilder.ResultsDirectory, $"--results-directory {dataCollectorSettingsBuilder.Quote("path")}");
        }

        [Test]
        public void Should_Set_ProjectDll_Quoted_When_WithProjectDll()
        {
            dataCollectorSettingsBuilder.WithProjectDll("projectdll");
            Assert.AreEqual(dataCollectorSettingsBuilder.ProjectDll, dataCollectorSettingsBuilder.Quote("projectdll"));
        }

        [Test]
        public void Should_Set_RunSettings_As_Quoted_GeneratedRunSettings_When_Initialize()
        {
            dataCollectorSettingsBuilder.Initialize(null, ".runsettings", "generated.runsettings");
            Assert.AreEqual(dataCollectorSettingsBuilder.RunSettings, $"--settings {dataCollectorSettingsBuilder.Quote("generated.runsettings")}");
        }

        #endregion

        [Test]
        public void Should_Have_Format_As_Cobertura()
        {
            Assert.AreEqual(dataCollectorSettingsBuilder.Format, "cobertura");
        }

        [Test]
        public void Should_Use_RunSettings_Exclude_If_Present()
        {
            Initialize();
            dataCollectorSettingsBuilder.WithExclude(new string[] { "from project" }, "from run settings");
            Assert.AreEqual(dataCollectorSettingsBuilder.Exclude, "from run settings");
        }

        [Test]
        public void Should_Use_Project_Exclude_If_No_RunSettings()
        {
            dataCollectorSettingsBuilder.WithExclude(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.Exclude, "first,second");
        }

        [Test]
        public void Should_Use_Project_Exclude_If_No_RunSettings_Null()
        {
            dataCollectorSettingsBuilder.WithExclude(null, null);
            Assert.IsNull(dataCollectorSettingsBuilder.Exclude);
        }

        [Test]
        public void Should_Fallback_To_Options_For_Exclude_If_Not_RunSettingsOnly()
        {
            Initialize(false);
            dataCollectorSettingsBuilder.WithExclude(new string[] { "first", "second" }, null);
            Assert.AreEqual("first,second", dataCollectorSettingsBuilder.Exclude);
        }

        [Test]
        public void Should_Not_Fallback_To_Options_For_Exclude_If_RunSettingsOnly()
        {
            Initialize(true);
            dataCollectorSettingsBuilder.WithExclude(new string[] { "first", "second" }, null);
            Assert.IsNull(dataCollectorSettingsBuilder.Exclude);
        }

        [Test]
        public void Should_Use_RunSettings_ExcludeByAttribute_If_Present()
        {
            Initialize();
            dataCollectorSettingsBuilder.WithExcludeByAttribute(new string[] { "from project" }, "from run settings");
            Assert.AreEqual("from run settings", dataCollectorSettingsBuilder.ExcludeByAttribute);
        }

        [Test]
        public void Should_Use_Project_ExcludeByAttribute_If_No_RunSettings()
        {
            dataCollectorSettingsBuilder.WithExcludeByAttribute(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.ExcludeByAttribute, "first,second");
        }

        [Test]
        public void Should_Use_Project_ExcludeByAttribute_If_No_RunSettings_Null()
        {
            dataCollectorSettingsBuilder.WithExcludeByAttribute(null, null);
            Assert.IsNull(dataCollectorSettingsBuilder.ExcludeByAttribute);
        }

        [Test]
        public void Should_Fallback_To_Options_For_ExcludeByAttribute_If_Not_RunSettingsOnly()
        {
            Initialize(false);
            dataCollectorSettingsBuilder.WithExcludeByAttribute(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.ExcludeByAttribute, "first,second");
        }

        [Test]
        public void Should_Not_Fallback_To_Options_For_ExcludeByAttribute_If_RunSettingsOnly()
        {
            Initialize(true);
            dataCollectorSettingsBuilder.WithExcludeByAttribute(new string[] { "first", "second" }, null);
            Assert.IsNull(dataCollectorSettingsBuilder.ExcludeByAttribute);
        }

        [Test]
        public void Should_Use_RunSettings_ExcludeByFile_If_Present()
        {
            Initialize();
            dataCollectorSettingsBuilder.WithExcludeByFile(new string[] { "from project" }, "from run settings");
            Assert.AreEqual(dataCollectorSettingsBuilder.ExcludeByFile, "from run settings");
        }

        [Test]
        public void Should_Use_Project_ExcludeByFile_If_No_RunSettings()
        {
            dataCollectorSettingsBuilder.WithExcludeByFile(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.ExcludeByFile, "first,second");
        }

        [Test]
        public void Should_Use_Project_ExcludeByFile_If_No_RunSettings_Null()
        {
            dataCollectorSettingsBuilder.WithExcludeByFile(null, null);
            Assert.IsNull(dataCollectorSettingsBuilder.ExcludeByFile);
        }

        [Test]
        public void Should_Fallback_To_Options_For_ExcludeByFile_If_Not_RunSettingsOnly()
        {
            Initialize(false);
            dataCollectorSettingsBuilder.WithExcludeByFile(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.ExcludeByFile, "first,second");
        }

        [Test]
        public void Should_Not_Fallback_To_Options_For_ExcludeByFile_If_RunSettingsOnly()
        {
            Initialize(true);
            dataCollectorSettingsBuilder.WithExcludeByFile(new string[] { "first", "second" }, null);
            Assert.IsNull(dataCollectorSettingsBuilder.ExcludeByFile);
        }

        [Test]
        public void Should_Use_RunSettings_Include_If_Present()
        {
            Initialize();
            dataCollectorSettingsBuilder.WithInclude(new string[] { "from project" }, "from run settings");
            Assert.AreEqual(dataCollectorSettingsBuilder.Include, "from run settings");
        }

        [Test]
        public void Should_Use_Project_Include_If_No_RunSettings()
        {
            dataCollectorSettingsBuilder.WithInclude(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.Include, "first,second");
        }

        [Test]
        public void Should_Use_Project_Include_If_No_RunSettings_Null()
        {
            dataCollectorSettingsBuilder.WithInclude(null, null);
            Assert.IsNull(dataCollectorSettingsBuilder.Include);
        }

        [Test]
        public void Should_Fallback_To_Options_For_Include_If_Not_RunSettingsOnly()
        {
            Initialize(false);
            dataCollectorSettingsBuilder.WithInclude(new string[] { "first", "second" }, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.Include, "first,second");
        }

        [Test]
        public void Should_Not_Fallback_To_Options_For_Include_If_RunSettingsOnly()
        {
            Initialize(true);
            dataCollectorSettingsBuilder.WithInclude(new string[] { "first", "second" }, null);
            Assert.IsNull(dataCollectorSettingsBuilder.Include);
        }

        [TestCase("true")]
        [TestCase("false")]
        public void Should_Use_RunSettings_IncludeTestAssembly_If_Present(string runSettingsIncludeTestAssembly)
        {
            Initialize();
            dataCollectorSettingsBuilder.WithIncludeTestAssembly(true, runSettingsIncludeTestAssembly);
            Assert.AreEqual(runSettingsIncludeTestAssembly, dataCollectorSettingsBuilder.IncludeTestAssembly);
        }

        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void Should_Use_Project_IncludeTestAssembly_If_No_RunSettings(bool optionsIncludeTestAssembly, string expected)
        {
            dataCollectorSettingsBuilder.WithIncludeTestAssembly(optionsIncludeTestAssembly, null);
            Assert.AreEqual(expected, dataCollectorSettingsBuilder.IncludeTestAssembly);
        }

        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void Should_Fallback_To_Options_For_IncludeTestAssembly_If_Not_RunSettingsOnly(bool optionsIncludeTestAssembly, string expected)
        {
            Initialize(false);
            dataCollectorSettingsBuilder.WithIncludeTestAssembly(optionsIncludeTestAssembly, null);
            Assert.AreEqual(dataCollectorSettingsBuilder.IncludeTestAssembly, expected);
        }

        [Test]
        public void Should_Not_Fallback_To_Options_For_IncludeTestAssembly_If_RunSettingsOnly()
        {
            Initialize(true);
            dataCollectorSettingsBuilder.WithIncludeTestAssembly(true, null);
            Assert.IsNull(dataCollectorSettingsBuilder.Include);
        }

        [Test]
        public void Should_Set_Corresponding_Property_For_RunSettings_Only_Elements()
        {
            dataCollectorSettingsBuilder.WithSingleHit("singlehit");
            dataCollectorSettingsBuilder.WithSkipAutoProps("skipautoprops");
            dataCollectorSettingsBuilder.WithIncludeDirectory("includedirectory");
            dataCollectorSettingsBuilder.WithUseSourceLink("sourcelink");

            Assert.AreEqual(dataCollectorSettingsBuilder.SingleHit, "singlehit");
            Assert.AreEqual(dataCollectorSettingsBuilder.SkipAutoProps, "skipautoprops");
            Assert.AreEqual(dataCollectorSettingsBuilder.IncludeDirectory, "includedirectory");
            Assert.AreEqual(dataCollectorSettingsBuilder.UseSourceLink, "sourcelink");
        }

        [TestCaseSource(nameof(BuildReturnsSettingsSource))]
        public void Should_Return_Dotnet_Test_Settings_When_Build(Action<DataCollectorSettingsBuilder> setUp, string expectedSettings)
        {
            dataCollectorSettingsBuilder.Initialize(new Mock<IAppOptions>().Object, null, generatedRunSettingsPath);
            setUp(dataCollectorSettingsBuilder);
            Assert.AreEqual(dataCollectorSettingsBuilder.Build(), expectedSettings);
        }

        static IEnumerable<TestCaseData> BuildReturnsSettingsSource()
        {
            Action<DataCollectorSettingsBuilder> allSettingsSetup = builder =>
            {
                builder.ProjectDll = "test.dll";
                builder.Blame = "blame";
                builder.NoLogo = "nologo";
                builder.Diagnostics = "diagnostics";
                builder.RunSettings = "runsettings";
                builder.ResultsDirectory = "resultsdirectory";
            };
            var allSettingsExpected = "test.dll blame nologo diagnostics runsettings resultsdirectory";
            return new List<TestCaseData>
            {
                new TestCaseData(allSettingsSetup, allSettingsExpected)
            };
        }

        [TestCaseSource(nameof(BuildGeneratesRunSettingsSource))]
        public void Should_Generate_RunSettings_When_Builds(Action<DataCollectorSettingsBuilder> setUp, string existingRunSettings, string expectedXml)
        {
            setUp(dataCollectorSettingsBuilder);

            if (existingRunSettings != null)
            {
                XDocument.Parse(existingRunSettings).Save(existingRunSettingsPath);
            }

            dataCollectorSettingsBuilder.Initialize(null, existingRunSettings == null ? null : existingRunSettingsPath, generatedRunSettingsPath);
            dataCollectorSettingsBuilder.Build();


            var diff = DiffBuilder.Compare(Input.FromDocument(XDocument.Load(generatedRunSettingsPath)))
             .WithTest(Input.From(expectedXml)).Build();

            Assert.IsFalse(diff.HasDifferences());

        }

        static IEnumerable<TestCaseData> BuildGeneratesRunSettingsSource()
        {
            Action<DataCollectorSettingsBuilder> fullSetup = (dataCollectorSettingsBuilder) =>
            {
                dataCollectorSettingsBuilder.Format = "coverlet";
                dataCollectorSettingsBuilder.Exclude = "exclude";
                dataCollectorSettingsBuilder.Include = "include";
                dataCollectorSettingsBuilder.ExcludeByAttribute = "excludebyattribute";
                dataCollectorSettingsBuilder.ExcludeByFile = "excludebyfile";
                dataCollectorSettingsBuilder.IncludeTestAssembly = "includetestassembly";
                dataCollectorSettingsBuilder.IncludeDirectory = "includedirectory";
                dataCollectorSettingsBuilder.SingleHit = "singlehit";
                dataCollectorSettingsBuilder.UseSourceLink = "sourcelink";
                dataCollectorSettingsBuilder.SkipAutoProps = "skipautoprops";
            };

            var expectedXml = @"<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat Code Coverage"">
                <Configuration>
                    <Format>coverlet</Format>
                    <Exclude>exclude</Exclude>
                    <Include>include</Include>
                    <ExcludeByAttribute>excludebyattribute</ExcludeByAttribute>
                    <ExcludeByFile>excludebyfile</ExcludeByFile>
                    <IncludeDirectory>includedirectory</IncludeDirectory>
                    <SingleHit>singlehit</SingleHit>
                    <UseSourceLink>sourcelink</UseSourceLink>
                    <IncludeTestAssembly>includetestassembly</IncludeTestAssembly>
                    <SkipAutoProps>skipautoprops</SkipAutoProps>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
";

            var noExistingRunSettingsTest = new TestCaseData(fullSetup, null, expectedXml);

            var existingCoverletCollector = @"<RunSettings>
      <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat Code Coverage"">
                <Configuration>
                    <Format>json</Format>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
";

            var withExistingReplaceDataCollectorTest = new TestCaseData(fullSetup, existingCoverletCollector, expectedXml);

            var noDataCollectionRunSettings = @"<RunSettings>
</RunSettings>
";

            var noDataCollectionRunSettingsTest = new TestCaseData(fullSetup, noDataCollectionRunSettings, expectedXml);

            var noDataCollectors = @"<RunSettings>
      <DataCollectionRunSettings>
    </DataCollectionRunSettings>
</RunSettings>
";
            var noDataCollectorsTest = new TestCaseData(fullSetup, noDataCollectors, expectedXml);

            var noCoverletCollector = @"<RunSettings>
      <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""Other"">
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
";

            var noCoverletCollectorExpectedXml = @"<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""Other"">
            </DataCollector>
            <DataCollector friendlyName=""XPlat Code Coverage"">
                <Configuration>
                    <Format>coverlet</Format>
                    <Exclude>exclude</Exclude>
                    <Include>include</Include>
                    <ExcludeByAttribute>excludebyattribute</ExcludeByAttribute>
                    <ExcludeByFile>excludebyfile</ExcludeByFile>
                    <IncludeDirectory>includedirectory</IncludeDirectory>
                    <SingleHit>singlehit</SingleHit>
                    <UseSourceLink>sourcelink</UseSourceLink>
                    <IncludeTestAssembly>includetestassembly</IncludeTestAssembly>
                    <SkipAutoProps>skipautoprops</SkipAutoProps>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
";

            var noCoverletCollectorTest = new TestCaseData(fullSetup, noCoverletCollector, noCoverletCollectorExpectedXml);

            Action<DataCollectorSettingsBuilder> partialSetup = (dataCollectorSettingsBuilder) =>
            {
                dataCollectorSettingsBuilder.Format = "coverlet";
            };

            var expectedNullElementXml = @"<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat Code Coverage"">
                <Configuration>
                    <Format>coverlet</Format>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
";

            var doesNotSetElementsWhenNullTest = new TestCaseData(partialSetup, null, expectedNullElementXml);

            return new List<TestCaseData>
            {
                noExistingRunSettingsTest,
                withExistingReplaceDataCollectorTest,
                noDataCollectionRunSettingsTest,
                noDataCollectorsTest,
                noCoverletCollectorTest,
                doesNotSetElementsWhenNullTest
            };
        }
    }
}