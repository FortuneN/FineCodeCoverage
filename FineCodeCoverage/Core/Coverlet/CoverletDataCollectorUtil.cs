using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{

    [Export(typeof(ICoverletDataCollectorUtil))]
    internal class CoverletDataCollectorUtil : ICoverletDataCollectorUtil
    {
        private readonly IFileUtil fileUtil;
        private readonly IRunSettingsCoverletConfigurationFactory runSettingsCoverletConfigurationFactory;
        private readonly ILogger logger;
        private readonly IProcessUtil processUtil;
        private readonly IDataCollectorSettingsBuilderFactory dataCollectorSettingsBuilderFactory;
        private readonly ICoverletDataCollectorGeneratedCobertura coverletDataCollectorGeneratedCobertura;
        private readonly IProcessResponseProcessor processResponseProcessor;
        private readonly ICoverletCollectorZipProvider coverletCollectorZipProvider;
        private readonly ICoverletCollectorFolder coverletCollectorFolder;


        //for tests
        internal IRunSettingsCoverletConfiguration runSettingsCoverletConfiguration;
        internal ICoverageProject coverageProject;
        private const string LogPrefix = "Coverlet Collector Run";
        internal string TestAdapterPathArg { get; set; }

        [ImportingConstructor]
        public CoverletDataCollectorUtil(
            IFileUtil fileUtil,
            IRunSettingsCoverletConfigurationFactory runSettingsCoverletConfigurationFactory, 
            ILogger logger, 
            IProcessUtil processUtil,
            IDataCollectorSettingsBuilderFactory dataCollectorSettingsBuilderFactory,
            ICoverletDataCollectorGeneratedCobertura coverletDataCollectorGeneratedCobertura,
            IProcessResponseProcessor processResponseProcessor,
            ICoverletCollectorZipProvider coverletCollectorZipProvider,
            ICoverletCollectorFolder coverletCollectorFolder
            )
        {
            this.fileUtil = fileUtil;
            this.runSettingsCoverletConfigurationFactory = runSettingsCoverletConfigurationFactory;
            this.logger = logger;
            this.processUtil = processUtil;
            this.dataCollectorSettingsBuilderFactory = dataCollectorSettingsBuilderFactory;
            this.coverletDataCollectorGeneratedCobertura = coverletDataCollectorGeneratedCobertura;
            this.processResponseProcessor = processResponseProcessor;
            this.coverletCollectorZipProvider = coverletCollectorZipProvider;
            this.coverletCollectorFolder = coverletCollectorFolder;
        }
        
        private bool? GetUseDataCollectorFromProjectFile()
        {
            var root = coverageProject.ProjectFileXElement;
            var propertyGroups = root.Elements().Where(el => el.Name.LocalName == "PropertyGroup");
            foreach (var propertyGroup in propertyGroups)
            {
                var useDataCollectorElement = propertyGroup.Elements().FirstOrDefault(ig => ig.Name.LocalName == "UseDataCollector");
                if (useDataCollectorElement != null)
                {
                    var useDataCollectorValue = useDataCollectorElement.Value.ToLower().Trim();
                    return useDataCollectorValue == "true" || useDataCollectorValue == "";
                }
                
            }
            return null;
        }
        
        private bool OverriddenFromProjectFile()
        {
            var useDataCollectorFromProjectFile = GetUseDataCollectorFromProjectFile();
            if (useDataCollectorFromProjectFile.HasValue)
            {
                return !useDataCollectorFromProjectFile.Value;
            }
            else
            {
                return false;
            }
        }

        private bool HasSetUseDataCollectorInProjectFile()
        {
            var useDataCollector = GetUseDataCollectorFromProjectFile();
            return useDataCollector.HasValue && useDataCollector.Value;
        }

        public bool CanUseDataCollector(ICoverageProject coverageProject)
        {
            runSettingsCoverletConfiguration = runSettingsCoverletConfigurationFactory.Create();
            this.coverageProject = coverageProject;
            
            if(coverageProject.RunSettingsFile != null)
            {
                var runSettingsXml = fileUtil.ReadAllText(coverageProject.RunSettingsFile);
                    
                runSettingsCoverletConfiguration.Read(runSettingsXml);
                switch (runSettingsCoverletConfiguration.CoverletDataCollectorState)
                {
                    case CoverletDataCollectorState.Disabled:
                        return false;
                    case CoverletDataCollectorState.Enabled:
                        return !OverriddenFromProjectFile();
                }

            }

            return HasSetUseDataCollectorInProjectFile();
            
        }

        private string GetSettings()
        {
            var dataCollectorSettingsBuilder = dataCollectorSettingsBuilderFactory.Create();
            dataCollectorSettingsBuilder
                .Initialize(coverageProject.Settings, coverageProject.RunSettingsFile, $"{coverageProject.CoverageOutputFolder}/FCC.runsettings");
            
            // command arguments
            dataCollectorSettingsBuilder
                .WithProjectDll(coverageProject.TestDllFile);
            dataCollectorSettingsBuilder
                .WithBlame();
            dataCollectorSettingsBuilder
                .WithNoLogo();
            dataCollectorSettingsBuilder
                .WithDiagnostics($"{coverageProject.CoverageOutputFolder}/diagnostics.log");
            
            dataCollectorSettingsBuilder
                .WithResultsDirectory(coverageProject.CoverageOutputFolder);

            string[] projectExcludes = coverageProject.ExcludedReferencedProjects.ToArray();
            if(coverageProject.Settings.Exclude != null)
            {
                projectExcludes = projectExcludes.Concat(coverageProject.Settings.Exclude).ToArray();
            }

            //DataCollector Configuration
            dataCollectorSettingsBuilder
                .WithExclude(projectExcludes, runSettingsCoverletConfiguration.Exclude);
            dataCollectorSettingsBuilder
                .WithExcludeByFile(coverageProject.Settings.ExcludeByFile, runSettingsCoverletConfiguration.ExcludeByFile);
            dataCollectorSettingsBuilder
                .WithExcludeByAttribute(coverageProject.Settings.ExcludeByAttribute, runSettingsCoverletConfiguration.ExcludeByAttribute);
            dataCollectorSettingsBuilder
                .WithInclude(coverageProject.Settings.Include, runSettingsCoverletConfiguration.Include);
            dataCollectorSettingsBuilder
                .WithIncludeTestAssembly(coverageProject.Settings.IncludeTestAssembly, runSettingsCoverletConfiguration.IncludeTestAssembly);

            dataCollectorSettingsBuilder
                .WithIncludeDirectory(runSettingsCoverletConfiguration.IncludeDirectory);
            dataCollectorSettingsBuilder
                .WithSingleHit(runSettingsCoverletConfiguration.SingleHit);
            dataCollectorSettingsBuilder
                .WithUseSourceLink(runSettingsCoverletConfiguration.UseSourceLink);
            dataCollectorSettingsBuilder
                .WithSkipAutoProps(runSettingsCoverletConfiguration.SkipAutoProps);
            
            return dataCollectorSettingsBuilder
                .Build();

        }

        public async Task<bool> RunAsync(bool throwError = false)
        {
            var settings = GetSettings();
            
            LogRun(settings);
            
            var result = await processUtil
            .ExecuteAsync(new ExecuteRequest
            {
                FilePath = "dotnet",
                Arguments = $@"test --collect:""XPlat Code Coverage"" {settings} --test-adapter-path {TestAdapterPathArg}",
                WorkingDirectory = coverageProject.ProjectOutputFolder
            });
            // this is how coverlet console determines exit code
            // https://github.com/coverlet-coverage/coverlet/blob/ac0e0fad2f0301a3fe9a3de9f8cdb32f406ce6d8/src/coverlet.console/Program.cs
            // https://github.com/coverlet-coverage/coverlet/issues/388

            // vstest
            // https://github.com/microsoft/vstest/blob/34fa5b59661c3d87c849e81fa5be68e3dec90b76/src/vstest.console/CommandLine/Executor.cs#L146

            // dotnet
            // https://github.com/dotnet/sdk/blob/936935f18c3540ed77c97e392780a9dd82aca441/src/Cli/dotnet/commands/dotnet-test/Program.cs#L86
            
            // test failure has exit code 1 
            return processResponseProcessor.Process(result, code => code == 0 || code == 1, throwError, GetLogTitle(), () =>
             {
                 coverletDataCollectorGeneratedCobertura.CorrectPath(coverageProject.CoverageOutputFolder, coverageProject.CoverageOutputFile);
             });

        }
        private string GetLogTitle()
        {
            return $"{LogPrefix} ({coverageProject.ProjectName})";
        }
        internal string LogRunMessage(string coverletSettings)
        {
            return $"{GetLogTitle()} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", coverletSettings)}";
        }
        private void LogRun(string coverletSettings)
        {
            logger.Log(LogRunMessage(coverletSettings));
        }

        public void Initialize(string appDataFolder)
        {
            var zipDestination = coverletCollectorFolder.EnsureUnzipped(appDataFolder, coverletCollectorZipProvider.ProvideZip());
            var testAdapterPath = Path.Combine(zipDestination, "build", "netstandard1.0");
            TestAdapterPathArg = $@"""{testAdapterPath}""";
        }
    }
}
