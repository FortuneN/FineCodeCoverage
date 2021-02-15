using System.IO;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Model;
using NUnit.Framework;

namespace Test
{
    public class RunSettingsCoverletConfiguration_Tests
    {
        [Test]
        public void Extract_Should_Return_False_When_No_Coverlet_DataCollector()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""Not XPlat code coverage"">
                <Configuration>
                    <Format> json,cobertura,lcov,teamcity,opencover </Format>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";
            Assert.False(runSettingsCoverletConfiguration.Extract(runSettingsXml));
        }

        [Test]
        public void Extract_Should_Return_False_When_No_Coverlet_DataCollector_Configuration()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            Assert.False(runSettingsCoverletConfiguration.Extract(runSettingsXml));
        }

        [Test]
        public void Extract_Should_Return_False_When_No_Coverlet_DataCollector_Configuration_Elements()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            Assert.False(runSettingsCoverletConfiguration.Extract(runSettingsXml));
        }

        [Test]
        public void Extract_Should_Return_False_When_Unknown_Configuration_Element()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                    <Unknown/>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            Assert.False(runSettingsCoverletConfiguration.Extract(runSettingsXml));
        }

        [Test]
        public void Extract_Should_Return_True_When_Known_Configuration_Element()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                    <Format/>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            Assert.True(runSettingsCoverletConfiguration.Extract(runSettingsXml));
        }

        [Test]
        public void Should_Set_Configuration_Properties()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                    <Format>format</Format>
                    <Exclude>exclude</Exclude>
                    <Include>include</Include>
                    <ExcludeByAttribute>excludebyattribute</ExcludeByAttribute>
                    <ExcludeByFile>excludebyfile</ExcludeByFile>
                    <IncludeDirectory>includedirectory</IncludeDirectory>
                    <SingleHit>singlehit</SingleHit>
                    <UseSourceLink>usesourcelink</UseSourceLink>
                    <IncludeTestAssembly>includetestassembly</IncludeTestAssembly>
                    <SkipAutoProps>skipautoprops</SkipAutoProps>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            runSettingsCoverletConfiguration.Extract(runSettingsXml);
            Assert.AreEqual(runSettingsCoverletConfiguration.Format, "format");
            Assert.AreEqual(runSettingsCoverletConfiguration.Exclude, "exclude");
            Assert.AreEqual(runSettingsCoverletConfiguration.Include, "include");
            Assert.AreEqual(runSettingsCoverletConfiguration.ExcludeByAttribute, "excludebyattribute");
            Assert.AreEqual(runSettingsCoverletConfiguration.ExcludeByFile, "excludebyfile");
            Assert.AreEqual(runSettingsCoverletConfiguration.IncludeDirectory, "includedirectory");
            Assert.AreEqual(runSettingsCoverletConfiguration.SingleHit, "singlehit");
            Assert.AreEqual(runSettingsCoverletConfiguration.UseSourceLink, "usesourcelink");
            Assert.AreEqual(runSettingsCoverletConfiguration.IncludeTestAssembly, "includetestassembly");
            Assert.AreEqual(runSettingsCoverletConfiguration.SkipAutoProps, "skipautoprops");

        }

        [Test]
        public void Should_Have_Null_Property_Values_For_Missing_Configuration_Elements()
        {
            var runSettingsCoverletConfiguration = new RunSettingsCoverletConfiguration();
            var runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                    <Format>format</Format>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            runSettingsCoverletConfiguration.Extract(runSettingsXml);
            
            Assert.IsNull(runSettingsCoverletConfiguration.Exclude);
            Assert.IsNull(runSettingsCoverletConfiguration.Include);
            Assert.IsNull(runSettingsCoverletConfiguration.ExcludeByAttribute);
            Assert.IsNull(runSettingsCoverletConfiguration.ExcludeByFile);
            Assert.IsNull(runSettingsCoverletConfiguration.IncludeDirectory);
            Assert.IsNull(runSettingsCoverletConfiguration.SingleHit);
            Assert.IsNull(runSettingsCoverletConfiguration.UseSourceLink);
            Assert.IsNull(runSettingsCoverletConfiguration.IncludeTestAssembly);
            Assert.IsNull(runSettingsCoverletConfiguration.SkipAutoProps);

            var runSettingsCoverletConfiguration2 = new RunSettingsCoverletConfiguration();
            var runSettingsXml2 = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName=""XPlat code coverage"">
                <Configuration>
                    <Exclude>exclude</Exclude>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>";

            runSettingsCoverletConfiguration2.Extract(runSettingsXml2);

            Assert.IsNull(runSettingsCoverletConfiguration2.Format);
        }

    }
}