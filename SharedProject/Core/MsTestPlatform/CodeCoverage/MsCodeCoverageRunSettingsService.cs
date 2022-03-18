using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
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
using FineCodeCoverage.Options;
using FineCodeCoverage.Engine.ReportGenerator;
using System.Threading.Tasks;
using System.Xml.Linq;
using FineCodeCoverage.Core.Utilities.VsThreading;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IMsCodeCoverageRunSettingsService))]
    [Export(typeof(IRunSettingsService))]
    internal class MsCodeCoverageRunSettingsService : IMsCodeCoverageRunSettingsService, IRunSettingsService
    {
        public string ShimPath { get; private set; }

        public string Name => "Fine Code Coverage MsCodeCoverageRunSettingsService";

        internal class UserRunSettingsProjectDetails : IUserRunSettingsProjectDetails
        {
            public IMsCodeCoverageOptions Settings { get; set; }
            public string OutputFolder { get; set; }
            public string TestDllFile { get; set; }
            public List<string> ExcludedReferencedProjects { get; set; }
            public List<string> IncludedReferencedProjects { get; set; }
        }

        private class CoverageProjectRunSettings : ICoverageProjectRunSettings
        {
            public ICoverageProject CoverageProject { get; set; }
            public string RunSettings { get; set; }
            public string CustomTemplatePath { get; internal set; }
        }

        private readonly string builtInRunSettingsTemplateString;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        private readonly IBuiltInRunSettingsTemplate builtInRunSettingsTemplate;
        private readonly ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider;
        private readonly IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory;
        private readonly IShimCopier shimCopier;
        private readonly ILogger logger;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private IFCCEngine fccEngine;
        private const string zipPrefix = "microsoft.codecoverage";
        private const string zipDirectoryName = "msCodeCoverage";
        private const string msCodeCoverageMessage = "Ms code coverage";
        private Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup;
        private readonly IProjectRunSettingsGenerator projectRunSettingsGenerator;
        private readonly IUserRunSettingsService userRunSettingsService;
        private string msCodeCoveragePath;
        private string shimPath;

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(
            IToolFolder toolFolder, 
            IToolZipProvider toolZipProvider, 
            IAppOptionsProvider appOptionsProvider,
            ICoverageToolOutputManager coverageOutputManager,
            IProjectRunSettingsGenerator projectRunSettingsGenerator,
            IUserRunSettingsService userRunSettingsService,
            IBuiltInRunSettingsTemplate builtInRunSettingsTemplate,
            ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider,
            IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory,
            IShimCopier shimCopier,
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
            this.runSettingsTemplateReplacementsFactory = runSettingsTemplateReplacementsFactory;
            this.shimCopier = shimCopier;
            this.logger = logger;
            this.reportGeneratorUtil = reportGeneratorUtil;
            builtInRunSettingsTemplateString = builtInRunSettingsTemplate.Template;
            this.projectRunSettingsGenerator = projectRunSettingsGenerator;
            this.userRunSettingsService = userRunSettingsService;
        }

        public void Initialize(string appDataFolder, IFCCEngine fccEngine, CancellationToken cancellationToken)
        {
            this.fccEngine = fccEngine;
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
            msCodeCoveragePath = Path.Combine(zipDestination, "build", "netstandard1.0");
            shimPath = Path.Combine(zipDestination, "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
        }
        
        #region set up for collection
        public async Task<MsCodeCoverageCollectionStatus> IsCollectingAsync(ITestOperation testOperation)
        {
            var collectionStatus = MsCodeCoverageCollectionStatus.NotCollecting;

            var coverageProjects = await testOperation.GetCoverageProjectsAsync();
            var coverageProjectsWithRunSettings = coverageProjects.Where(coverageProject => coverageProject.RunSettingsFile != null).ToList();

            var useMsCodeCoverage = appOptionsProvider.Get().MsCodeCoverage;
            var (suitable,specifiedMsCodeCoverage) = userRunSettingsService.CheckUserRunSettingsSuitability(
                coverageProjectsWithRunSettings.Select(cp => cp.RunSettingsFile),useMsCodeCoverage
            );

            if (suitable)
            {
                await PrepareCoverageProjectsAsync(coverageProjects);
                SetUserRunSettingsProjectDetails(coverageProjectsWithRunSettings);
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

            return collectionStatus;
        }

        private async Task PrepareCoverageProjectsAsync(List<ICoverageProject> coverageProjects)
        {
            coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            foreach (var coverageProject in coverageProjects)
            {
                await coverageProject.PrepareForCoverageAsync(CancellationToken.None, false);
            }
        }

        internal Dictionary<string, IUserRunSettingsProjectDetails> SetUserRunSettingsProjectDetails(List<ICoverageProject> coverageProjectsWithRunSettings)
        {
            userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>();
            foreach (var coverageProjectWithRunSettings in coverageProjectsWithRunSettings)
            {
                var userRunSettingsProjectDetails = new UserRunSettingsProjectDetails
                {
                    Settings = coverageProjectWithRunSettings.Settings,
                    OutputFolder = coverageProjectWithRunSettings.ProjectOutputFolder,
                    TestDllFile = coverageProjectWithRunSettings.TestDllFile,
                    ExcludedReferencedProjects = coverageProjectWithRunSettings.ExcludedReferencedProjects,
                    IncludedReferencedProjects = coverageProjectWithRunSettings.IncludedReferencedProjects
                };
                userRunSettingsProjectDetailsLookup.Add(coverageProjectWithRunSettings.TestDllFile, userRunSettingsProjectDetails);
            }
            return userRunSettingsProjectDetailsLookup;
        }

        public async Task<(bool Success, List<string> CustomTemplatePaths)> PrepareRunSettingsAsync(List<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            shimCopier.Copy(shimPath,coverageProjects);

            return await GenerateProjectsRunSettingsAsync(coverageProjects, solutionDirectory);
        }

        private async Task<(bool Success, List<string> CustomTemplatePaths)> GenerateProjectsRunSettingsAsync(IEnumerable<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            var successfullyGeneratedRunSettings = false;
            IEnumerable<CoverageProjectRunSettings> projectsRunSettings = null;
            try
            {
                projectsRunSettings = GetProjectsRunSettingsWriteDetails(coverageProjects, solutionDirectory);
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
            var customTemplatePaths = projectsRunSettings.Select(projectRunSettings => projectRunSettings.CustomTemplatePath).ToList();
            try
            {
                await projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(projectsRunSettings);
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

        private List<CoverageProjectRunSettings> GetProjectsRunSettingsWriteDetails(IEnumerable<ICoverageProject> coverageProjects, string solutionDirectory)
        {
            return coverageProjects.Select(coverageProject => 
            {
                var projectDirectory = Path.GetDirectoryName(coverageProject.ProjectFile);
                var (runSettingsTemplate, customTemplatePath) = GetRunSettingsTemplate(projectDirectory, solutionDirectory);
                var projectRunSettings = CreateProjectRunSettings(coverageProject, runSettingsTemplate);

                return new CoverageProjectRunSettings { CoverageProject = coverageProject, RunSettings = projectRunSettings, CustomTemplatePath = customTemplatePath };
                
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

            var replacements = runSettingsTemplateReplacementsFactory.Create(settings, coverageProject.CoverageOutputFolder, settings.Enabled.ToString(), modulePathsInclude, modulePathsExclude, msCodeCoveragePath);

            var projectRunSettings = builtInRunSettingsTemplate.Replace(runSettingsTemplate, replacements);

            return XDocument.Parse(projectRunSettings).FormatXml();
        }
        #endregion

        #region IRunSettingsService
        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Microsoft.VisualStudio.TestWindow.Extensibility.ILogger log)
        {
            if (configurationInfo.RequestState == RunSettingConfigurationInfoState.Execution && !builtInRunSettingsTemplate.FCCGenerated(inputRunSettingDocument))
            {
                var replacements = runSettingsTemplateReplacementsFactory.Create(configurationInfo.TestContainers, userRunSettingsProjectDetailsLookup, msCodeCoveragePath);
                return userRunSettingsService.AddFCCRunSettings(builtInRunSettingsTemplate, replacements, inputRunSettingDocument);
            }
            return null;
        }
        #endregion

        internal IThreadHelper threadHelper = new VsThreadHelper();

        public async Task CollectAsync(IOperation operation, ITestOperation testOperation)
        {
            var resultsUris = operation.GetRunSettingsMsDataCollectorResultUri();
            var coberturaFiles = new string[0];
            if (resultsUris != null)
            {
                coberturaFiles = resultsUris.Select(uri => uri.LocalPath).Where(f => f.EndsWith(".cobertura.xml")).ToArray();
            }

            if (coberturaFiles.Length == 0)
            {
                await CombinedLogAsync("No cobertura files for ms code coverage.");
            }

            fccEngine.RunAndProcessReport(coberturaFiles,() =>
            {
                threadHelper.JoinableTaskFactory.Run(async () =>
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
            await threadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            action();
        }
        #endregion

    }
}
