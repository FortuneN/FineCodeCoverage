using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IDataCollectorSettingsBuilder))]
    internal class DataCollectorSettingsBuilder : IDataCollectorSettingsBuilder
    {
        private string generatedRunSettingsPath;
        private string existingRunSettings;
        private IAppOptions coverageProjectSettings;

        #region Arguments
        internal string ProjectDll { get; set; }
        internal string Blame { get; set; }
        internal string NoLogo { get; set; }


        internal string Diagnostics { get; set; }
        internal string RunSettings { get; set; }
        internal string ResultsDirectory { get; set; }
        #endregion

        #region DataCollector Configuration

        internal string Format { get; set; } = "cobertura";

        internal string Exclude { get; set; }
        internal string ExcludeByAttribute { get; set; }

        internal string ExcludeByFile { get; set; }

        internal string Include { get; set; }

        internal string IncludeTestAssembly { get; set; }

        internal string IncludeDirectory { get; set; }

        internal string SingleHit { get; set; }

        internal string SkipAutoProps { get; set; }

        internal string UseSourceLink { get; set; }
        #endregion

        public string Build()
        {
            GenerateRunSettings();
            var args = new List<string>
            {
                ProjectDll,
                Blame,
                NoLogo,
                Diagnostics,
                RunSettings,
                ResultsDirectory
            }.Where(a => !string.IsNullOrEmpty(a));
            return string.Join(" ", args);
        }

        #region run settings xml generation
        private void GenerateRunSettings()
        {
            var runSettingsDocument = existingRunSettings == null ? GenerateFullRunSettings() : GenerateRunSettingsFromExisting();
            runSettingsDocument.Save(generatedRunSettingsPath);
        }

        private XDocument GenerateFullRunSettings()
        {
            return new XDocument(new XElement("RunSettings",
                DataCollectionRunSettings()));

        }
        private XElement DataCollectionRunSettings()
        {
            return new XElement("DataCollectionRunSettings",
                    DataCollectors());
        }
        private XElement DataCollectors()
        {
            return new XElement("DataCollectors", GenerateDataCollectorElement());
        }

        private XDocument GenerateRunSettingsFromExisting()
        {
            var existingRunSettingsDocument = XDocument.Load(existingRunSettings);
            var existingRunSettingsElement = existingRunSettingsDocument.Root;
            var dataCollectionRunSettings = existingRunSettingsElement.Element("DataCollectionRunSettings");
            if (dataCollectionRunSettings == null)
            {
                existingRunSettingsElement.Add(DataCollectionRunSettings());
            }
            else
            {
                var dataCollectors = dataCollectionRunSettings.Element("DataCollectors");
                if (dataCollectors == null)
                {
                    dataCollectionRunSettings.Add(DataCollectors());
                }
                else
                {
                    var coverletCollectorElement = dataCollectors.Elements("DataCollector").FirstOrDefault(e =>
                    {
                        var friendlyNameAttribute = e.Attribute("friendlyName");
                        if (friendlyNameAttribute != null)
                        {
                            return friendlyNameAttribute.Value.ToLower() == "xplat code coverage";
                        }
                        return false;
                    });
                    var newCoverletCollector = GenerateDataCollectorElement();
                    if (coverletCollectorElement != null)
                    {
                        coverletCollectorElement.ReplaceWith(newCoverletCollector);
                    }
                    else
                    {
                        dataCollectors.Add(newCoverletCollector);
                    }
                }
            }
            return existingRunSettingsDocument;

        }

        private string GetElementIfNotNull(string elementName, string value)
        {
            return value == null ? "" : $"<{elementName}>{value}</{elementName}>";
        }
        private XElement GenerateDataCollectorElement()
        {
            var configurationElement = $@"<Configuration>
                {GetElementIfNotNull("Format", Format)}
                {GetElementIfNotNull("Exclude", Exclude)}
                {GetElementIfNotNull("Include", Include)}
                {GetElementIfNotNull("ExcludeByAttribute", ExcludeByAttribute)}
                {GetElementIfNotNull("ExcludeByFile", ExcludeByFile)}
                {GetElementIfNotNull("IncludeDirectory", IncludeDirectory)}
                {GetElementIfNotNull("SingleHit", SingleHit)}
                {GetElementIfNotNull("UseSourceLink", UseSourceLink)}
                {GetElementIfNotNull("IncludeTestAssembly", IncludeTestAssembly)}
                {GetElementIfNotNull("SkipAutoProps", SkipAutoProps)}
</Configuration>
";

            return new XElement("DataCollector", new XAttribute("friendlyName", "XPlat Code Coverage"),
                XElement.Parse(configurationElement));

        }
        #endregion

        internal string Quote(string settings)
        {
            return $@"""{settings}""";
        }

        #region With args
        public void WithBlame()
        {
            Blame = "--blame";
        }

        public void WithDiagnostics(string logPath)
        {
            Diagnostics = $"--diag {Quote(logPath)}";
        }

        public void WithNoLogo()
        {
            NoLogo = "--nologo";
        }

        public void WithProjectDll(string projectDll)
        {
            ProjectDll = Quote(projectDll);
        }

        public void WithResultsDirectory(string resultsDirectory)
        {
            ResultsDirectory = $"--results-directory {Quote(resultsDirectory)}";
        }

        public void Initialize(IAppOptions coverageProjectSettings, string runSettingsPath, string generatedRunSettingsPath)
        {
            this.coverageProjectSettings = coverageProjectSettings;
            this.generatedRunSettingsPath = generatedRunSettingsPath;
            existingRunSettings = runSettingsPath;
            RunSettings = $"--settings {Quote(generatedRunSettingsPath)}";
        }
        #endregion

        #region existing run settings or options
        private string RunSettingsOrProject(string[] project, string runSettings)
        {
            string DelimitProject()
            {
                if (project == null)
                {
                    return null;
                }
                return string.Join(",", project);
            }

            if (existingRunSettings == null)
            {
                return DelimitProject();
            }

            if (runSettings != null)
            {
                return runSettings;
            }

            if (!coverageProjectSettings.RunSettingsOnly) // default true
            {
                return DelimitProject();
            }
            return null;
        }

        public void WithExclude(string[] projectExclude, string runSettingsExclude)
        {
            Exclude = RunSettingsOrProject(projectExclude, runSettingsExclude);
        }

        public void WithExcludeByAttribute(string[] projectExcludeByAttribute, string runSettingsExcludeByAttribute)
        {
            ExcludeByAttribute = RunSettingsOrProject(projectExcludeByAttribute, runSettingsExcludeByAttribute);
        }

        public void WithExcludeByFile(string[] projectExcludeByFile, string runSettingsExcludeByFile)
        {
            ExcludeByFile = RunSettingsOrProject(projectExcludeByFile, runSettingsExcludeByFile);
        }

        public void WithInclude(string[] projectInclude, string runSettingsInclude)
        {
            Include = RunSettingsOrProject(projectInclude, runSettingsInclude);
        }

        public void WithIncludeTestAssembly(bool projectIncludeTestAssembly, string runSettingsIncludeTestAssembly)
        {
            string ProjectInclude()
            {
                return projectIncludeTestAssembly.ToString().ToLower();
            }

            string includeTestAssembly = null;
            if (existingRunSettings == null)
            {
                includeTestAssembly = ProjectInclude();
            }
            else
            {
                if (runSettingsIncludeTestAssembly != null)
                {
                    includeTestAssembly = runSettingsIncludeTestAssembly;
                }
                else
                {
                    if (!coverageProjectSettings.RunSettingsOnly) // default true
                    {
                        includeTestAssembly = ProjectInclude();
                    }
                }
            }


            IncludeTestAssembly = includeTestAssembly;
        }
        #endregion

        #region Coverlet Collector specific
        public void WithIncludeDirectory(string includeDirectory)
        {
            IncludeDirectory = includeDirectory;
        }

        public void WithSingleHit(string singleHit)
        {
            SingleHit = singleHit;
        }

        public void WithUseSourceLink(string useSourceLink)
        {
            UseSourceLink = useSourceLink;
        }

        public void WithSkipAutoProps(string skipAutoProps)
        {
            SkipAutoProps = skipAutoProps;

        }
        #endregion
    }
}
