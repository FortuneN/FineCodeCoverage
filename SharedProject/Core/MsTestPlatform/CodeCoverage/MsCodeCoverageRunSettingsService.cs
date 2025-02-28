using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
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
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IMsCodeCoverageRunSettingsService))]
    [Export(typeof(IRunSettingsService))]
    internal class MsCodeCoverageRunSettingsService : IMsCodeCoverageRunSettingsService, IRunSettingsService
    {
        public string Name => "Fine Code Coverage MsCodeCoverageRunSettingsService";

        private class UserRunSettingsProjectDetails : IUserRunSettingsProjectDetails
        {
            public IMsCodeCoverageOptions Settings { get; set; }
            public string CoverageOutputFolder { get; set; }
            public string TestDllFile { get; set; }
            public List<IReferencedProject> ExcludedReferencedProjects { get; set; }
            public List<IReferencedProject> IncludedReferencedProjects { get; set; }
        }
        private class CoverageProjectsByType
        {
            public List<ICoverageProject> All { get; private set; }
            public List<ICoverageProject> RunSettings { get; private set; }
            public List<ICoverageProject> Templated { get; private set; }

            public bool HasTemplated()
            {
                return Templated.Any();
            }

            public static async Task<CoverageProjectsByType> CreateAsync(ITestOperation testOperation)
            {
                var coverageProjects = await testOperation.GetCoverageProjectsAsync();
                var coverageProjectsWithRunSettings = coverageProjects.Where(coverageProject => coverageProject.RunSettingsFile != null).ToList();
                var coverageProjectsWithoutRunSettings = coverageProjects.Except(coverageProjectsWithRunSettings).ToList();
                return new CoverageProjectsByType
                {
                    All = coverageProjects,
                    RunSettings = coverageProjectsWithRunSettings,
                    Templated = coverageProjectsWithoutRunSettings
                };
            }
        }

        private readonly IToolUnzipper toolUnzipper;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageToolOutputManager coverageOutputManager;
        private readonly IShimCopier shimCopier;
        private readonly ILogger logger;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private IFCCEngine fccEngine;

        private const string zipPrefix = "microsoft.codecoverage";
        private const string zipDirectoryName = "msCodeCoverage";

        private const string msCodeCoverageMessage = "Ms code coverage";
        internal Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup; // for tests

        private readonly IUserRunSettingsService userRunSettingsService;
        private readonly ITemplatedRunSettingsService templatedRunSettingsService;
        private string fccMsTestAdapterPath;
        private string shimPath;

        private CoverageProjectsByType coverageProjectsByType;
        private bool useMsCodeCoverage;
        
        internal MsCodeCoverageCollectionStatus collectionStatus; // for tests
        private RunMsCodeCoverage runMsCodeCoverage;

        private bool IsCollecting => collectionStatus == MsCodeCoverageCollectionStatus.Collecting;

        internal IThreadHelper threadHelper = new VsThreadHelper();

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(
            IToolUnzipper toolUnzipper, 
            IAppOptionsProvider appOptionsProvider,
            ICoverageToolOutputManager coverageOutputManager,
            IUserRunSettingsService userRunSettingsService,
            ITemplatedRunSettingsService templatedRunSettingsService,
            IShimCopier shimCopier,
            ILogger logger,
            IReportGeneratorUtil reportGeneratorUtil
            )
        {
            this.toolUnzipper = toolUnzipper;
            this.appOptionsProvider = appOptionsProvider;
            this.coverageOutputManager = coverageOutputManager;
            this.shimCopier = shimCopier;
            this.logger = logger;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.userRunSettingsService = userRunSettingsService;
            this.templatedRunSettingsService = templatedRunSettingsService;
        }

        public void Initialize(string appDataFolder, IFCCEngine fccEngine, CancellationToken cancellationToken)
        {
            this.fccEngine = fccEngine;
            var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolder, zipDirectoryName,zipPrefix, cancellationToken);
            fccMsTestAdapterPath = Path.Combine(zipDestination, "build", "netstandard2.0");
            shimPath = Path.Combine(zipDestination, "build", "netstandard2.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
        }
        
        #region set up for collection
       
        public async Task<MsCodeCoverageCollectionStatus> IsCollectingAsync(ITestOperation testOperation)
        {
            await InitializeIsCollectingAsync(testOperation);
            if( runMsCodeCoverage == RunMsCodeCoverage.No)
            {
                logger.Log($"See option {nameof(IAppOptions.RunMsCodeCoverage)} for a better ( Beta ) experience.  {FCCGithub.Readme}");
                reportGeneratorUtil.LogCoverageProcess($"See option {nameof(IAppOptions.RunMsCodeCoverage)} for a better ( Beta ) experience. View readme.");
            }
            else
            {
                await TrySetUpForCollectionAsync(testOperation.SolutionDirectory);
            }
            
            ReportEndOfCoverageRunIfError();
            return collectionStatus;
        }

        private async Task TrySetUpForCollectionAsync(string solutionDirectory)
        {
            IUserRunSettingsAnalysisResult analysisResult = await TryAnalyseUserRunSettingsAsync();
            if (analysisResult.Ok())
            {
                await SetUpForCollectionAsync(
                    analysisResult.ProjectsWithFCCMsTestAdapter,
                    analysisResult.SpecifiedMsCodeCoverage,
                    solutionDirectory
                );
            }
        }

        private async Task SetUpForCollectionAsync(
            List<ICoverageProject> coverageProjectsForShim, 
            bool specifiedMsCodeCoverageInRunSettings,
            string solutionDirectory
        )
        {
            await PrepareCoverageProjectsAsync();
            SetUserRunSettingsProjectDetails();
            
            await GenerateTemplatedRunSettingsIfRequiredAsync(
                specifiedMsCodeCoverageInRunSettings,
                coverageProjectsForShim,
                solutionDirectory
            );
            CopyShimWhenCollecting(coverageProjectsForShim);
        }

        private void ReportEndOfCoverageRunIfError()
        {
            if (collectionStatus == MsCodeCoverageCollectionStatus.Error)
            {
                reportGeneratorUtil.EndOfCoverageRun();
            }
        }

        private Task InitializeIsCollectingAsync(ITestOperation testOperation)
        {
            runMsCodeCoverage = appOptionsProvider.Get().RunMsCodeCoverage;
            useMsCodeCoverage = runMsCodeCoverage == RunMsCodeCoverage.Yes;
            userRunSettingsProjectDetailsLookup = null;
            return CleanUpAsync(testOperation);
        }

        private async Task<IUserRunSettingsAnalysisResult> TryAnalyseUserRunSettingsAsync()
        {
            IUserRunSettingsAnalysisResult analysisResult = null;
            try
            {
                analysisResult = await AnalyseUserRunSettingsAsync();
            }
            catch (Exception exc)
            {
                await ExceptionAnalysingUserRunSettingsAsync(exc);
            }

            return analysisResult;
        }

        private Task ExceptionAnalysingUserRunSettingsAsync(Exception exc)
        {
            collectionStatus = MsCodeCoverageCollectionStatus.Error;
            return CombinedLogExceptionAsync(exc, "Exception analysing runsettings files");
        }

        private async Task<IUserRunSettingsAnalysisResult> AnalyseUserRunSettingsAsync()
        {
            var analysisResult = userRunSettingsService.Analyse(
                    coverageProjectsByType.RunSettings,
                    useMsCodeCoverage,
                    fccMsTestAdapterPath
                );

            if (analysisResult.Suitable)
            {
                await CollectingIfUserRunSettingsOnlyAsync();
            }

            return analysisResult;
        }

        private async Task GenerateTemplatedRunSettingsIfRequiredAsync(
            bool runSettingsSpecifiedMsCodeCoverage, 
            List<ICoverageProject> coverageProjectsForShim, 
            string solutionDirectory
        )
        {
            if (ShouldGenerateTemplatedRunSettings(runSettingsSpecifiedMsCodeCoverage))
            {
                await GenerateTemplatedRunSettingsAsync(coverageProjectsForShim, solutionDirectory);
            }
        }
        private async Task GenerateTemplatedRunSettingsAsync(
            List<ICoverageProject> coverageProjectsForShim,
            string solutionDirectory
        )
        {
            var generationResult = await templatedRunSettingsService.GenerateAsync(
                coverageProjectsByType.Templated,
                solutionDirectory,
                fccMsTestAdapterPath
            );

            await ProcessTemplateGenerationResultAsync(generationResult, coverageProjectsForShim);
        }


        private bool ShouldGenerateTemplatedRunSettings(bool runSettingsSpecifiedMsCodeCoverage)
        {
            return coverageProjectsByType.HasTemplated() && (useMsCodeCoverage || runSettingsSpecifiedMsCodeCoverage);
        }

        private async Task ProcessTemplateGenerationResultAsync(IProjectRunSettingsFromTemplateResult generationResult, List<ICoverageProject> coverageProjectsForShim)
        {
            if (generationResult.ExceptionReason == null)
            {
                await CollectingWithTemplateAsync(generationResult, coverageProjectsForShim);
            }
            else
            {
                var exceptionReason = generationResult.ExceptionReason;
                await CombinedLogExceptionAsync(exceptionReason.Exception, exceptionReason.Reason);
                collectionStatus = MsCodeCoverageCollectionStatus.Error;
            }
        }

        private async Task CollectingWithTemplateAsync(IProjectRunSettingsFromTemplateResult generationResult, List<ICoverageProject> coverageProjectsForShim)
        {
            coverageProjectsForShim.AddRange(generationResult.CoverageProjectsWithFCCMsTestAdapter);
            await CombinedLogAsync(() =>
            {
                var leadingMessage = generationResult.CustomTemplatePaths.Any() ? $"{msCodeCoverageMessage} - custom template paths" : msCodeCoverageMessage;
                var loggerMessages = new List<string> { leadingMessage }.Concat(generationResult.CustomTemplatePaths.Distinct());
                logger.Log(loggerMessages);
                reportGeneratorUtil.LogCoverageProcess(msCodeCoverageMessage);
            });
            collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
        }

        private async Task CollectingIfUserRunSettingsOnlyAsync()
        {
            if (!coverageProjectsByType.HasTemplated())
            {
                collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
                await CombinedLogAsync($"{msCodeCoverageMessage} with user runsettings");
            }
        }

        private void CopyShimWhenCollecting(List<ICoverageProject> coverageProjectsForShim)
        {
            if (IsCollecting)
            {
                shimCopier.Copy(shimPath, coverageProjectsForShim);
            }
        }

        private async Task PrepareCoverageProjectsAsync()
        {
            coverageOutputManager.SetProjectCoverageOutputFolder(coverageProjectsByType.All);
            foreach (var coverageProject in coverageProjectsByType.All)
            {
                await coverageProject.PrepareForCoverageAsync(CancellationToken.None, false);
            }
        }

        private void SetUserRunSettingsProjectDetails()
        {
            userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>();
            foreach (var coverageProjectWithRunSettings in coverageProjectsByType.RunSettings)
            {
                var userRunSettingsProjectDetails = new UserRunSettingsProjectDetails
                {
                    Settings = coverageProjectWithRunSettings.Settings,
                    CoverageOutputFolder = coverageProjectWithRunSettings.CoverageOutputFolder,
                    TestDllFile = coverageProjectWithRunSettings.TestDllFile,
                    ExcludedReferencedProjects = coverageProjectWithRunSettings.ExcludedReferencedProjects,
                    IncludedReferencedProjects = coverageProjectWithRunSettings.IncludedReferencedProjects
                };
                userRunSettingsProjectDetailsLookup.Add(coverageProjectWithRunSettings.TestDllFile, userRunSettingsProjectDetails);
            }
        }
        
        #endregion

        #region IRunSettingsService
        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Microsoft.VisualStudio.TestWindow.Extensibility.ILogger log)
        {
            if (configurationInfo.IsTestExecution() && ShouldAddFCCRunSettings())
            {
                return userRunSettingsService.AddFCCRunSettings(inputRunSettingDocument, configurationInfo, userRunSettingsProjectDetailsLookup, fccMsTestAdapterPath);
            }
            return null;
        }

        private bool ShouldAddFCCRunSettings()
        {
            return IsCollecting && userRunSettingsProjectDetailsLookup != null && userRunSettingsProjectDetailsLookup.Count > 0;
        }

        #endregion

        public async Task CollectAsync(IOperation operation, ITestOperation testOperation)
        {
            await CleanUpAsync(testOperation);

            var coberturaFiles = GetCoberturaFiles(operation);
            if (coberturaFiles.Length == 0)
            {
                await CombinedLogAsync("No cobertura files for ms code coverage.");
            }

            fccEngine.RunAndProcessReport(coberturaFiles);
        }

        private string[] GetCoberturaFiles(IOperation operation)
        {
            var resultsUris = operation.GetRunSettingsMsDataCollectorResultUri();
            var coberturaFiles = new string[0];
            if (resultsUris != null)
            {
                coberturaFiles = resultsUris.Select(uri => uri.LocalPath).Where(f => f.EndsWith(".cobertura.xml")).ToArray();
            }
            return coberturaFiles;
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

        private Task CombinedLogExceptionAsync(Exception ex, string reason)
        {
            return CombinedLogAsync(() =>
            {
                logger.Log(reason, ex.ToString());
                reportGeneratorUtil.LogCoverageProcess(reason);
            });
        }

        #endregion

        public Task TestExecutionNotFinishedAsync(ITestOperation testOperation)
        {
            return CleanUpAsync(testOperation);
        }

        private async Task CleanUpAsync(ITestOperation testOperation)
        {
            coverageProjectsByType = await CoverageProjectsByType.CreateAsync(testOperation);
            await templatedRunSettingsService.CleanUpAsync(coverageProjectsByType.RunSettings);
            collectionStatus = MsCodeCoverageCollectionStatus.NotCollecting;
        }
    }

    public static class IRunSettingsConfigurationInfoExtensions { 
        public static bool IsTestExecution(this IRunSettingsConfigurationInfo configurationInfo)
        {
            return configurationInfo.RequestState == RunSettingConfigurationInfoState.Execution;
        }

    }

    internal static class UserRunSettingsAnalysisResultExtensions
    {
        public static bool Ok(this IUserRunSettingsAnalysisResult userRunSettingsAnalysisResult)
        {
            if (userRunSettingsAnalysisResult == null)
            {
                return false;
            }
            return userRunSettingsAnalysisResult.Suitable;

        }
    }

}
