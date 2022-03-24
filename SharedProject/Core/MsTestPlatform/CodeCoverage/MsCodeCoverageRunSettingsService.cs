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
            public string OutputFolder { get; set; }
            public string TestDllFile { get; set; }
            public List<string> ExcludedReferencedProjects { get; set; }
            public List<string> IncludedReferencedProjects { get; set; }
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

        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
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
        private bool IsCollecting => collectionStatus == MsCodeCoverageCollectionStatus.Collecting;

        internal IThreadHelper threadHelper = new VsThreadHelper();

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(
            IToolFolder toolFolder, 
            IToolZipProvider toolZipProvider, 
            IAppOptionsProvider appOptionsProvider,
            ICoverageToolOutputManager coverageOutputManager,
            IUserRunSettingsService userRunSettingsService,
            ITemplatedRunSettingsService templatedRunSettingsService,
            IShimCopier shimCopier,
            ILogger logger,
            IReportGeneratorUtil reportGeneratorUtil
            )
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
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
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
            fccMsTestAdapterPath = Path.Combine(zipDestination, "build", "netstandard1.0");
            shimPath = Path.Combine(zipDestination, "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
        }
        
        #region set up for collection
       
        public async Task<MsCodeCoverageCollectionStatus> IsCollectingAsync(ITestOperation testOperation)
        {
            await InitializeIsCollectingAsync(testOperation);

            IUserRunSettingsAnalysisResult analysisResult = await TryAnalyseUserRunSettingsAsync();
            if (analysisResult != null)
            {
                var coverageProjectsForShim = analysisResult.ProjectsWithFCCMsTestAdapter;

                if (analysisResult.Suitable)
                {
                    await PrepareCoverageProjectsAsync();
                    SetUserRunSettingsProjectDetails();
                    if (coverageProjectsByType.HasTemplated())
                    {
                        await GenerateTemplatedAsync(
                            analysisResult.SpecifiedMsCodeCoverage,
                            coverageProjectsForShim,
                            testOperation.SolutionDirectory
                        );
                    }
                    else
                    {
                        await CollectingUserRunSettingsAsync();
                    }
                }

                CopyShimWhenCollecting(coverageProjectsForShim);
            }
            
            ReportEndOfCoverageRunIfError();
            return collectionStatus;
        }

        private void ReportEndOfCoverageRunIfError()
        {
            if (collectionStatus == MsCodeCoverageCollectionStatus.Error)
            {
                reportGeneratorUtil.EndOfCoverageRun();
            }
        }

        private async Task InitializeIsCollectingAsync(ITestOperation testOperation)
        {
            collectionStatus = MsCodeCoverageCollectionStatus.NotCollecting;
            useMsCodeCoverage = appOptionsProvider.Get().MsCodeCoverage;
            coverageProjectsByType = await CoverageProjectsByType.CreateAsync(testOperation);
            userRunSettingsProjectDetailsLookup = null;
            await templatedRunSettingsService.CleanUpAsync(coverageProjectsByType.RunSettings);
        }

        private async Task<IUserRunSettingsAnalysisResult> TryAnalyseUserRunSettingsAsync()
        {
            IUserRunSettingsAnalysisResult analysisResult = null;
            try
            {
                analysisResult = userRunSettingsService.Analyse(
                    coverageProjectsByType.RunSettings,
                    useMsCodeCoverage,
                    fccMsTestAdapterPath
                );
            }
            catch (Exception exc)
            {
                collectionStatus = MsCodeCoverageCollectionStatus.Error;
                await CombinedLogExceptionAsync(exc, "Exception analysing runsettings files");
            }
            return analysisResult;
        }

        private async Task GenerateTemplatedAsync(bool runSettingsSpecifiedMsCodeCoverage, List<ICoverageProject> coverageProjectsForShim, string solutionDirectory)
        {
            if (useMsCodeCoverage || runSettingsSpecifiedMsCodeCoverage)
            {
                var generationResult = await templatedRunSettingsService.GenerateAsync(
                    coverageProjectsByType.Templated, 
                    solutionDirectory, 
                    fccMsTestAdapterPath
                );

                if (generationResult.ExceptionReason == null)
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
                else
                {
                    var exceptionReason = generationResult.ExceptionReason;
                    await CombinedLogExceptionAsync(exceptionReason.Exception, exceptionReason.Reason);
                    collectionStatus = MsCodeCoverageCollectionStatus.Error;
                }
            }
        }

        private Task CollectingUserRunSettingsAsync()
        {
            collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
            return CombinedLogAsync($"{msCodeCoverageMessage} with user runsettings");
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
                    OutputFolder = coverageProjectWithRunSettings.ProjectOutputFolder,
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
            var coverageProjectsByType = await CoverageProjectsByType.CreateAsync(testOperation);
            await templatedRunSettingsService.CleanUpAsync(coverageProjectsByType.RunSettings);
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

            fccEngine.RunAndProcessReport(coberturaFiles);
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

        public Task TestExecutionNotFinishedAsync()
        {
            return templatedRunSettingsService.CleanUpAsync(coverageProjectsByType.Templated);
        }

    }

    public static class IRunSettingsConfigurationInfoExtensions { 
        public static bool IsTestExecution(this IRunSettingsConfigurationInfo configurationInfo)
        {
            return configurationInfo.RequestState == RunSettingConfigurationInfoState.Execution;
        }
        
    }


}
