using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Xml.XPath;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using FineCodeCoverage.Options;
using FineCodeCoverage.Engine.ReportGenerator;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    internal enum MsCodeCoverageCollectionStatus { NotCollecting, Collecting, Error}

    [Export(typeof(IMsCodeCoverageRunSettingsService))]
    [Export(typeof(IRunSettingsService))]
    internal class MsCodeCoverageRunSettingsService : IMsCodeCoverageRunSettingsService, IRunSettingsService
    {
        public string MsCodeCoveragePath { get; internal set; }

        public string ShimPath { get; private set; }

        public string Name => "Fine Code Coverage MsCodeCoverageRunSettingsService";

        internal class UserRunSettingsProjectDetails
        {
            public IMsCodeCoverageOptions Settings { get; set; }
            public string OutputFolder { get; set; }
            public string TestDllFile { get; set; }
            public List<string> ExcludedReferencedProjects { get; set; }
            public List<string> IncludedReferencedProjects { get; set; }
        }

        private class MergedIncludesExcludesOptions : IMsCodeCoverageIncludesExcludesOptions
        {
            private readonly List<IMsCodeCoverageIncludesExcludesOptions> allOptions;
            public MergedIncludesExcludesOptions(IEnumerable<IMsCodeCoverageIncludesExcludesOptions> allOptions)
            {
                this.allOptions = allOptions.ToList();

                ModulePathsExclude = Merge(options => options.ModulePathsExclude);
                ModulePathsInclude = Merge(options => options.ModulePathsInclude);
                CompanyNamesExclude = Merge(options => options.CompanyNamesExclude);
                CompanyNamesInclude = Merge(options => options.CompanyNamesInclude);
                PublicKeyTokensExclude = Merge(options => options.PublicKeyTokensExclude);
                PublicKeyTokensInclude = Merge(options => options.PublicKeyTokensInclude);
                SourcesExclude = Merge(options => options.SourcesExclude);
                SourcesInclude = Merge(options => options.SourcesInclude);
                AttributesExclude = Merge(options => options.AttributesExclude);
                AttributesInclude = Merge(options => options.AttributesInclude);
                FunctionsExclude = Merge(options => options.FunctionsExclude);
                FunctionsInclude = Merge(options => options.FunctionsInclude);
            }

            private string[] Merge(Func<IMsCodeCoverageIncludesExcludesOptions,string[]> selector)
            {
                return allOptions.SelectMany(options => selector(options) ?? Array.Empty<string>()).ToArray();
            }

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
        }

        private class RunSettingsTemplateReplacements : IRunSettingsTemplateReplacements
        {
            public string Enabled { get; set; }
            public string ResultsDirectory { get; set; }
            public string TestAdapter { get; set; }
            public string ModulePathsExclude { get; set; }
            public string ModulePathsInclude { get; set; }
            public string FunctionsExclude { get; set; }
            public string FunctionsInclude { get; set; }
            public string AttributesExclude { get; set; }
            public string AttributesInclude { get; set; }
            public string SourcesExclude { get; set; }
            public string SourcesInclude { get; set; }
            public string CompanyNamesExclude { get; set; }
            public string CompanyNamesInclude { get; set; }
            public string PublicKeyTokensExclude { get; set; }
            public string PublicKeyTokensInclude { get; set; }

            public RunSettingsTemplateReplacements(
                IMsCodeCoverageIncludesExcludesOptions settings,
                string resultsDirectory,
                string enabled,
                IEnumerable<string> modulePathsInclude,
                IEnumerable<string> modulePathsExclude,
                string testAdapter
            )
            {
                string GetExcludeIncludeElementsString(IEnumerable<string> excludeIncludes, string elementName)
                {
                    if (excludeIncludes == null)
                    {
                        return string.Empty;
                    }

                    var elements = excludeIncludes.Select(excludeInclude => $"<{elementName}>{excludeInclude}</{elementName}>").Distinct();
                    return string.Join("", elements);
                }


                ResultsDirectory = resultsDirectory;
                TestAdapter = testAdapter;
                Enabled = enabled;
                ModulePathsExclude = GetExcludeIncludeElementsString(modulePathsExclude, "ModulePath");
                ModulePathsInclude = GetExcludeIncludeElementsString(modulePathsInclude, "ModulePath");
                FunctionsExclude = GetExcludeIncludeElementsString(settings.FunctionsExclude, "Function");
                FunctionsInclude = GetExcludeIncludeElementsString(settings.FunctionsInclude, "Function");
                AttributesExclude = GetExcludeIncludeElementsString(settings.AttributesExclude, "Attribute");
                AttributesInclude = GetExcludeIncludeElementsString(settings.AttributesInclude, "Attribute");
                SourcesExclude = GetExcludeIncludeElementsString(settings.SourcesExclude, "Source");
                SourcesInclude = GetExcludeIncludeElementsString(settings.SourcesInclude, "Source");
                CompanyNamesExclude = GetExcludeIncludeElementsString(settings.CompanyNamesExclude, "CompanyName");
                CompanyNamesInclude = GetExcludeIncludeElementsString(settings.CompanyNamesInclude, "CompanyName");
                PublicKeyTokensExclude = GetExcludeIncludeElementsString(settings.PublicKeyTokensExclude, "PublicKeyToken");
                PublicKeyTokensInclude = GetExcludeIncludeElementsString(settings.PublicKeyTokensInclude, "PublicKeyToken");
            }
        }

        private class ShimCopier
        {
            private string ShimPath;
            public ShimCopier(string shimPath)
            {
                ShimPath = shimPath;
            }

            private void CopyShim(string outputFolder)
            {
                string destination = Path.Combine(outputFolder, Path.GetFileName(ShimPath));
                if (!File.Exists(destination))
                {
                    File.Copy(ShimPath, destination);
                }
            }

            private void CopyShimForNetFrameworkProjects(List<ICoverageProject> coverageProjects)
            {
                var netFrameworkCoverageProjects = coverageProjects.Where(cp => !cp.IsDotNetSdkStyle());
                foreach (var netFrameworkCoverageProject in netFrameworkCoverageProjects)
                {
                    CopyShim(netFrameworkCoverageProject.ProjectOutputFolder);
                }
            }

            public void Copy(List<ICoverageProject> coverageProjects)
            {
                CopyShimForNetFrameworkProjects(coverageProjects);
            }
        }

        private readonly string builtInRunSettingsTemplateString;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        private readonly IBuiltInRunSettingsTemplate builtInRunSettingsTemplate;
        private readonly ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider;
        private readonly ILogger logger;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private IFCCEngine fccEngine;
        private const string zipPrefix = "microsoft.codecoverage";
        private const string zipDirectoryName = "msCodeCoverage";
        private const string msCodeCoverageMessage = "Ms code coverage";
        internal Dictionary<string, UserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup;
        private ShimCopier shimCopier;
        private ProjectRunSettingsGenerator projectRunSettingsGenerator;

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(
            IToolFolder toolFolder, 
            IToolZipProvider toolZipProvider, 
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IAppOptionsProvider appOptionsProvider,
            ICoverageToolOutputManager coverageOutputManager,
            IBuiltInRunSettingsTemplate builtInRunSettingsTemplate,
            ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider,
            ILogger logger,
            IReportGeneratorUtil reportGeneratorUtil
            )
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
            this.appOptionsProvider = appOptionsProvider;
            this.coverageOutputManager = coverageOutputManager;
            this.builtInRunSettingsTemplate = builtInRunSettingsTemplate;
            this.customRunSettingsTemplateProvider = customRunSettingsTemplateProvider;
            this.logger = logger;
            this.reportGeneratorUtil = reportGeneratorUtil;
            builtInRunSettingsTemplateString = builtInRunSettingsTemplate.Template;
            this.projectRunSettingsGenerator = new ProjectRunSettingsGenerator(serviceProvider);
        }

        public void Initialize(string appDataFolder, IFCCEngine fccEngine, CancellationToken cancellationToken)
        {
            this.fccEngine = fccEngine;
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
            MsCodeCoveragePath = Path.Combine(zipDestination, "build", "netstandard1.0");
            shimCopier = new ShimCopier(Path.Combine(zipDestination, "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll"));
        }

        
        #region set up for collection
        public MsCodeCoverageCollectionStatus IsCollecting(ITestOperation testOperation)
        {
            var collectionStatus = MsCodeCoverageCollectionStatus.NotCollecting;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var useMsCodeCoverage = appOptionsProvider.Get().MsCodeCoverage;
                var coverageProjects = await testOperation.GetCoverageProjectsAsync();
                var coverageProjectsWithRunSettings = coverageProjects.Where(coverageProject => coverageProject.RunSettingsFile != null).ToList();

                var (suitable,specifiedMsCodeCoverage) = CheckUserRunSettingsSuitability(
                    coverageProjectsWithRunSettings.Select(cp => cp.RunSettingsFile),useMsCodeCoverage
                );

                if (suitable)
                {
                    await PrepareCoverageProjectsAsync(coverageProjects);
                    var projectsWithoutRunSettings = coverageProjects.Except(coverageProjectsWithRunSettings).ToList();
                    if (projectsWithoutRunSettings.Any())
                    {
                        if (specifiedMsCodeCoverage || useMsCodeCoverage)
                        {
                            var (successFullyPreparedRunSettings, customTemplatePaths) = await PrepareRunSettingsAsync(coverageProjects,testOperation.SolutionDirectory);
                            if (successFullyPreparedRunSettings)
                            {
                                await CombinedLogAsync(() =>
                                {
                                    var leadingMessage = customTemplatePaths.Any() ? $"{msCodeCoverageMessage} - custom template paths" : msCodeCoverageMessage;
                                    var loggerMessages = new List<string> { leadingMessage }.Concat(customTemplatePaths.Distinct());
                                    logger.Log(loggerMessages);
                                    reportGeneratorUtil.LogCoverageProcess(msCodeCoverageMessage);
                                });
                                collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
                            }
                            else
                            {
                                collectionStatus = MsCodeCoverageCollectionStatus.Error;
                            }
                        }
                    }
                    else
                    {
                        collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
                        await CombinedLogAsync($"{msCodeCoverageMessage} with user runsettings");
                    }
                }
            });

            return collectionStatus;
        }

        #region user runsettings suitability
        private static (bool Suitable, bool SpecifiedMsCodeCoverage) CheckUserRunSettingsSuitability(IEnumerable<string> userRunSettingsFiles, bool useMsCodeCoverage)
        {
            var specifiedMsCodeCoverage = false;
            foreach (var userRunSettingsFile in userRunSettingsFiles)
            {
                var (suitable, projectSpecifiedMsCodeCoverage) = ValidateUserRunSettings(File.ReadAllText(userRunSettingsFile), useMsCodeCoverage);
                if (!suitable)
                {
                    return (false, false);
                }
                if (projectSpecifiedMsCodeCoverage)
                {
                    specifiedMsCodeCoverage = true;
                }
            }

            return (true, specifiedMsCodeCoverage);
        }

        internal static (bool Suitable, bool SpecifiedMsCodeCoverage) ValidateUserRunSettings(string runSettings, bool useMsCodeCoverage)
        {
            try
            {
                var runSettingsDoc = XDocument.Parse(runSettings);
                var dataCollectorsElement = runSettingsDoc.GetStrictDescendant("RunSettings/DataCollectionRunSettings/DataCollectors");
                if (dataCollectorsElement == null)
                {
                    return (useMsCodeCoverage, false);
                }

                var msDataCollectorElement = RunSettingsHelper.FindMsDataCollector(dataCollectorsElement);

                if (msDataCollectorElement == null)
                {
                    return (useMsCodeCoverage, false);
                }

                if (HasCoberturaFormat(msDataCollectorElement))
                {
                    return (true, true);
                }

                return (useMsCodeCoverage, true);
            }
            catch
            {
                return (false, false);
            }
        }

        private static bool HasCoberturaFormat(XElement msDataCollectorElement)
        {
            var formatElement = msDataCollectorElement.GetStrictDescendant("Configuration/Format");
            if (formatElement == null)
            {
                return false;
            }
            return formatElement.Value == "Cobertura";
        }

        #endregion
        
        private async Task PrepareCoverageProjectsAsync(List<ICoverageProject> coverageProjects)
        {
            userRunSettingsProjectDetailsLookup = new Dictionary<string, UserRunSettingsProjectDetails>();
            coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            foreach (var coverageProject in coverageProjects)
            {
                await coverageProject.PrepareForCoverageAsync(CancellationToken.None, false);
                var userRunSettingsProjectDetails = new UserRunSettingsProjectDetails
                {
                    Settings = coverageProject.Settings,
                    OutputFolder = coverageProject.ProjectOutputFolder,
                    TestDllFile = coverageProject.TestDllFile,
                    ExcludedReferencedProjects = coverageProject.ExcludedReferencedProjects,
                    IncludedReferencedProjects = coverageProject.IncludedReferencedProjects
                };
                userRunSettingsProjectDetailsLookup.Add(coverageProject.TestDllFile, userRunSettingsProjectDetails);
            }
        }

        public async Task<(bool Success, List<string> CustomTemplatePaths)> PrepareRunSettingsAsync(List<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            shimCopier.Copy(coverageProjects);

            return await GenerateProjectsRunSettingsAsync(coverageProjects, solutionDirectory);
        }

        private async Task<(bool Success, List<string> CustomTemplatePaths)> GenerateProjectsRunSettingsAsync(IEnumerable<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            var successfullyGeneratedRunSettings = false;
            List<(string projectRunSettings, string projectRunSettingsFilePath, Guid projectGuid, string customTemplatePath)> projectsRunSettingsWriteDetails = null;
            try
            {
                projectsRunSettingsWriteDetails = GetProjectsRunSettingsWriteDetails(coverageProjects, solutionDirectory);
            }
            catch (Exception ex)
            {
                await CombinedLogAsync(() =>
                {
                    var msg = "Exception generating ms runsettings";
                    logger.Log(msg, ex.ToString());
                    reportGeneratorUtil.LogCoverageProcess(msg);
                });
                return (false, null);
            }
            var customTemplatePaths = new List<string>();
            try
            {
                customTemplatePaths = await projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(projectsRunSettingsWriteDetails);
                successfullyGeneratedRunSettings = true;
            }
            catch (Exception ex)
            {
                await CombinedLogAsync(() =>
                {
                    var msg = "Exception writing ms runsettings";
                    logger.Log(msg, ex.ToString());
                    reportGeneratorUtil.LogCoverageProcess(msg);
                });
                await projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(coverageProjects);
            }
            return (successfullyGeneratedRunSettings, customTemplatePaths);
        }

        private List<(string projectRunSettings, string projectRunSettingsFilePath, Guid projectGuid, string customTemplatePath)> GetProjectsRunSettingsWriteDetails(IEnumerable<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            return coverageProjects.Select(coverageProject =>
            {
                var projectDirectory = Path.GetDirectoryName(coverageProject.ProjectFile);
                var (runSettingsTemplate, customTemplatePath) = GetRunSettingsTemplate(projectDirectory, solutionDirectory);
                var projectRunSettings = CreateProjectRunSettings(coverageProject, runSettingsTemplate);

                var projectRunSettingsFilePath = projectRunSettingsGenerator.GeneratedProjectRunSettingsFilePath(coverageProject);
                return (projectRunSettings, projectRunSettingsFilePath, coverageProject.Id, customTemplatePath);
            }).ToList();
        }

        private (string Template, string CustomPath) GetRunSettingsTemplate(string projectDirectory, string solutionDirectory)
        {
            string customPath = null;
            string template;
            var customRunSettingsTemplateDetails = customRunSettingsTemplateProvider.Provide(projectDirectory, solutionDirectory);
            if (customRunSettingsTemplateDetails != null)
            {
                customPath = customRunSettingsTemplateDetails.Path;
                template = builtInRunSettingsTemplate.ConfigureCustom(customRunSettingsTemplateDetails.Template);
            }
            else
            {
                template = builtInRunSettingsTemplateString;
            }
            return (template, customPath);
        }

        private string CreateProjectRunSettings(ICoverageProject coverageProject, string runSettingsTemplate)
        {
            var settings = coverageProject.Settings;
            var modulePathsExclude = coverageProject.ExcludedReferencedProjects.Select(
                rp => MsCodeCoverageRegex.RegexModuleName(rp)).Concat(settings.ModulePathsExclude ?? Enumerable.Empty<string>()).ToList();

            if (!settings.IncludeTestAssembly)
            {
                modulePathsExclude.Add(MsCodeCoverageRegex.RegexEscapePath(coverageProject.TestDllFile));
            }

            var modulePathsInclude = coverageProject.IncludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)).Concat(settings.ModulePathsInclude ?? Enumerable.Empty<string>()).ToList();

            var replacements = new RunSettingsTemplateReplacements(settings, coverageProject.CoverageOutputFolder, settings.Enabled.ToString(), modulePathsInclude, modulePathsExclude, MsCodeCoveragePath);

            var projectRunSettings = builtInRunSettingsTemplate.Replace(runSettingsTemplate, replacements);

            return XDocument.Parse(projectRunSettings).FormatXml();
        }
        #endregion

        #region IRunSettingsService
        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Microsoft.VisualStudio.TestWindow.Extensibility.ILogger log)
        {
            if (configurationInfo.RequestState == RunSettingConfigurationInfoState.Execution && NotFCCGenerated(inputRunSettingDocument))
            {
                return AddFCCSettings(inputRunSettingDocument, configurationInfo);
            }
            return null;
        }

        internal IXPathNavigable AddFCCSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            navigator.MoveToFirstChild();
            var clonedNavigator = navigator.Clone();
            IRunSettingsTemplateReplacements runSettingsTemplateReplacements = CreateReplacements(configurationInfo);
            ConfigureRunConfiguration(navigator, runSettingsTemplateReplacements);
            EnsureMsDataCollector(clonedNavigator, runSettingsTemplateReplacements);

            return inputRunSettingDocument;
        }

        private IRunSettingsTemplateReplacements CreateReplacements(IRunSettingsConfigurationInfo configurationInfo)
        {
            var allProjectDetails = configurationInfo.TestContainers.Select(tc => userRunSettingsProjectDetailsLookup[tc.Source]).ToList();
            // might have an issue with &resultsdirectory& as there is a ResultsDirectory property ?
            var resultsDirectory = allProjectDetails[0].OutputFolder;
            var allSettings = allProjectDetails.Select(pd => pd.Settings);
            var mergedSettings = new MergedIncludesExcludesOptions(allSettings);


            var modulePathsExclude = mergedSettings.ModulePathsExclude.Concat(allProjectDetails.SelectMany(pd =>
            {
                var additional = pd.ExcludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)).ToList();
                if (!pd.Settings.IncludeTestAssembly)
                {
                    additional.Add(MsCodeCoverageRegex.RegexEscapePath(pd.TestDllFile));
                }
                return additional;

            }));

            var modulePathsInclude = mergedSettings.ModulePathsInclude.Concat(
                allProjectDetails.SelectMany(projectDetails => projectDetails.IncludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)))
            );

            return new RunSettingsTemplateReplacements(mergedSettings, resultsDirectory, "true", modulePathsInclude, modulePathsExclude, MsCodeCoveragePath);
        }

        private void ConfigureRunConfiguration(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var movedToRunConfiguration = xpathNavigator.MoveToChild("RunConfiguration", "");
            if (movedToRunConfiguration)
            {
                if (!xpathNavigator.HasChild("TestAdaptersPaths"))
                {
                    xpathNavigator.AppendChild(builtInRunSettingsTemplate.TestAdaptersPathElement);
                }
                // todo ResultsDirectory ?

            }
            else
            {
                xpathNavigator.PrependChild(builtInRunSettingsTemplate.RunConfigurationElement);
                xpathNavigator.MoveToChild("RunConfiguration", "");
            }

            xpathNavigator.OuterXml = builtInRunSettingsTemplate.Replace(xpathNavigator.OuterXml, replacements);
        }

        private void EnsureMsDataCollector(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var addedMsDataCollector = true;
            var movedToDataCollectionRunSettings = xpathNavigator.MoveToChild("DataCollectionRunSettings", "");
            if (movedToDataCollectionRunSettings)
            {
                var movedToDataCollectors = xpathNavigator.MoveToChild("DataCollectors", "");
                if (movedToDataCollectors)
                {
                    XPathNavigator msDataCollectorNavigator = MoveToMsDataCollectorFromDataCollectors(xpathNavigator);

                    if (msDataCollectorNavigator != null)
                    {
                        addedMsDataCollector = false;
                        FixUpMsDataCollector(msDataCollectorNavigator, replacements);
                    }
                    else
                    {
                        xpathNavigator.AppendChild(builtInRunSettingsTemplate.MsDataCollectorElement);
                    }
                }
                else
                {
                    xpathNavigator.AppendChild(builtInRunSettingsTemplate.DataCollectorsElement);
                }
            }
            else
            {
                xpathNavigator.AppendChild(builtInRunSettingsTemplate.DataCollectionRunSettingsElement);
            }

            if (addedMsDataCollector)
            {
                xpathNavigator.MoveToRoot();
                var dataCollectorsNavigator = xpathNavigator.SelectSingleNode("/RunSettings/DataCollectionRunSettings/DataCollectors");
                var msDataCollectorNavigator = MoveToMsDataCollectorFromDataCollectors(dataCollectorsNavigator);
                ReplaceExcludesIncludes(msDataCollectorNavigator, replacements);
            }

        }

        private XPathNavigator MoveToMsDataCollectorFromDataCollectors(XPathNavigator navigator)
        {
            XPathNavigator msDataCollectorNavigator = null;
            var dataCollectorsIterator = navigator.SelectChildren("DataCollector", "");
            while (dataCollectorsIterator.MoveNext())
            {
                var currentNavigator = dataCollectorsIterator.Current;
                if (NavigatorOnMsDataCollector(currentNavigator))
                {
                    msDataCollectorNavigator = currentNavigator;
                    break;
                }
            }
            return msDataCollectorNavigator;
        }

        private bool NavigatorOnMsDataCollector(XPathNavigator navigator)
        {
            if (!navigator.HasAttributes)
            {
                return false;
            }
            var friendlyName = navigator.GetAttribute(RunSettingsHelper.FriendlyNameAttributeName, "");
            if (RunSettingsHelper.IsFriendlyMsCodeCoverage(friendlyName))
            {
                return true;
            }
            var uri = navigator.GetAttribute(RunSettingsHelper.UriAttributeName, "");
            return RunSettingsHelper.IsMsCodeCoverageUri(uri);
        }

        private void ReplaceExcludesIncludes(XPathNavigator msDataCollectorNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var toReplace = msDataCollectorNavigator.OuterXml;
            var replaced = builtInRunSettingsTemplate.Replace(toReplace, replacements);
            msDataCollectorNavigator.OuterXml = replaced;
        }

        private void FixUpMsDataCollector(XPathNavigator navigator, IRunSettingsTemplateReplacements replacements)
        {
            EnsureCorrectCoberturaFormat(navigator);
            ReplaceExcludesIncludes(navigator.Clone(), replacements);
        }

        private void EnsureCorrectCoberturaFormat(XPathNavigator navigator)
        {
            var movedToConfiguration = navigator.MoveToChild("Configuration", "");
            if (movedToConfiguration)
            {
                var movedToFormat = navigator.MoveToChild("Format", "");
                if (movedToFormat)
                {
                    if (navigator.InnerXml != "Cobertura")
                    {
                        navigator.InnerXml = "Cobertura";
                    }
                }
                else
                {
                    navigator.AppendChild("<Format>Cobertura</Format>");
                }
            }
            else
            {
                navigator.AppendChild("<Configuration><Format>Cobertura</Format></Configuration>");
            }
        }

        private bool NotFCCGenerated(IXPathNavigable inputRunSettingDocument)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            return navigator.SelectSingleNode($"//{builtInRunSettingsTemplate.FCCMarkerElementName}") == null;
        }
        #endregion

        public void Collect(IOperation operation, ITestOperation testOperation)
        {
            var resultsUris = operation.GetRunSettingsMsDataCollectorResultUri();
            var coberturaFiles = new string[0];
            if (resultsUris != null)
            {
                coberturaFiles = resultsUris.Select(uri => uri.LocalPath).Where(f => f.EndsWith(".cobertura.xml")).ToArray();
            }

            if (coberturaFiles.Length == 0)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await CombinedLogAsync("No cobertura files for ms code coverage.");
                });
            }

            fccEngine.RunAndProcessReport(coberturaFiles,() =>
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    List<ICoverageProject> coverageProjects = await testOperation.GetCoverageProjectsAsync();
                    await projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(coverageProjects);
                });
            });
        }

        public void StopCoverage()
        {
            fccEngine.StopCoverage();
        }

        #region Logging
        private async Task CombinedLogAsync(string message)
        {
            await CombinedLogAsync(() =>
            {
                logger.Log(message);
                reportGeneratorUtil.LogCoverageProcess(message);
            });
        }

        private async Task CombinedLogAsync(Action action)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            action();
        }
        #endregion

    }

    internal class ProjectRunSettingsGenerator
    {
        private readonly IServiceProvider serviceProvider;
        private const string fccGeneratedRunSettingsSuffix = "fcc-mscodecoverage-generated";

        public ProjectRunSettingsGenerator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task RemoveGeneratedProjectSettingsAsync(IEnumerable<ICoverageProject> coverageProjects)
        {
            var coverageProjectsForRemoval = coverageProjects.Where(coverageProject => IsGeneratedRunSettings(coverageProject.RunSettingsFile)).ToList();
            if (coverageProjectsForRemoval.Any())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
                Assumes.Present(vsSolution);
                foreach (var coverageProjectForRemoval in coverageProjectsForRemoval)
                {
                    await Task.Yield();
                    VsRunSettingsWriter.RemoveRunSettingsFilePath(vsSolution, coverageProjectForRemoval.Id);
                }
            }
        }

        public string GeneratedProjectRunSettingsFilePath(ICoverageProject coverageProject)
        {
            return Path.Combine(coverageProject.CoverageOutputFolder, $"{coverageProject.ProjectName}-{fccGeneratedRunSettingsSuffix}.runsettings");
        }

        private static void WriteProjectRunSettings(IVsSolution vsSolution, Guid projectGuid, string projectRunSettingsFilePath, string projectRunSettings)
        {
            if (VsRunSettingsWriter.WriteRunSettingsFilePath(vsSolution, projectGuid, projectRunSettingsFilePath))
            {
                File.WriteAllText(projectRunSettingsFilePath, projectRunSettings);
            }
        }

        public async Task<List<string>> WriteProjectsRunSettingsAsync(List<(string projectRunSettings, string projectRunSettingsFilePath, Guid projectGuid, string customTemplatePath)> projectsRunSettingsWriteDetails)
        {
            var customTemplatePaths = new List<string>();
            if (projectsRunSettingsWriteDetails.Any())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
                Assumes.Present(vsSolution);
                foreach (var (projectRunSettings, projectRunSettingsFilePath, projectGuid, customTemplatePath) in projectsRunSettingsWriteDetails)
                {
                    WriteProjectRunSettings(vsSolution, projectGuid, projectRunSettingsFilePath, projectRunSettings);
                    customTemplatePaths.Add(customTemplatePath);
                }
            }
            return customTemplatePaths;
        }

        private static bool IsGeneratedRunSettings(string runSettingsFile)
        {
            if (runSettingsFile == null)
            {
                return false;
            }

            return Path.GetFileNameWithoutExtension(runSettingsFile).EndsWith(fccGeneratedRunSettingsSuffix);
        }

    }

    internal class VsRunSettingsWriter
    {
        private static string projectRunSettingsFilePathElement = "RunSettingsFilePath";
        public static bool WriteRunSettingsFilePath(IVsSolution vsSolution, Guid projectGuid, string projectRunSettingsFilePath)
        {
            var success = false;
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vsSolution.GetProjectOfGuid(ref projectGuid, out var vsHierarchy) == VSConstants.S_OK)
            {
                if (vsHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
                {
                    // care not to use 2 !
                    success = vsBuildPropertyStorage.SetPropertyValue(projectRunSettingsFilePathElement, null, 1, projectRunSettingsFilePath) == VSConstants.S_OK;
                }
            }
            return success;
        }

        public static void RemoveRunSettingsFilePath(IVsSolution vsSolution, Guid projectGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (vsSolution.GetProjectOfGuid(ref projectGuid, out var vsHierarchy) == VSConstants.S_OK)
            {
                if (vsHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
                {
                    vsBuildPropertyStorage.RemoveProperty(projectRunSettingsFilePathElement, null, 1);
                }
            }
        }

    }

    internal static class MsCodeCoverageRegex
    {
        public static string RegexEscapePath(string path)
        {
            return path.Replace(@"\", @"\\");
        }

        public static string RegexModuleName(string moduleName)
        {
            return $".*\\\\{moduleName}.dll^";
        }
    }

    internal static class XPathNavigatorExtensions
    {
        public static bool HasChild(this XPathNavigator navigator, string elementName, string nsUri = "")
        {
            return navigator.Clone().MoveToChild(elementName, nsUri);
        }
    }
}
