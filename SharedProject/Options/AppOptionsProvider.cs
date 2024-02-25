using System;
using System.ComponentModel.Composition;
using System.Reflection;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Settings;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IAppOptionsProvider))]
    [Export(typeof(IAppOptionsStorageProvider))]
    internal class AppOptionsProvider : IAppOptionsProvider, IAppOptionsStorageProvider
    {
        private readonly ILogger logger;
        private readonly IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider;
        private readonly IJsonConvertService jsonConvertService;
        private readonly PropertyInfo[] appOptionsPropertyInfos;

        public event Action<IAppOptions> OptionsChanged;

        [ImportingConstructor]
        public AppOptionsProvider(
            ILogger logger, 
            IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider,
            IJsonConvertService jsonConvertService
            )
        {
            this.logger = logger;
            this.writableUserSettingsStoreProvider = writableUserSettingsStoreProvider;
            this.jsonConvertService = jsonConvertService;
            appOptionsPropertyInfos =typeof(IAppOptions).GetPublicProperties();
        }

        public void RaiseOptionsChanged(IAppOptions appOptions)
        {
            OptionsChanged?.Invoke(appOptions);
        }

        public IAppOptions Get()
        {
            var options = new AppOptions();
            LoadSettingsFromStorage(options);
            return options;
        }

        private WritableSettingsStore EnsureStore()
        {
            var settingsStore = writableUserSettingsStoreProvider.Provide();
            if (!settingsStore.CollectionExists(Vsix.Code))
            {
                settingsStore.CreateCollection(Vsix.Code);
            }
            return settingsStore;
        }

        private void AddDefaults(IAppOptions appOptions)
        {
            appOptions.ShowToolWindowToolbar = true;
            appOptions.NamespacedClasses = true;
            appOptions.ThresholdForCrapScore = 15;
            appOptions.ThresholdForNPathComplexity = 200;
            appOptions.ThresholdForCyclomaticComplexity = 30;
            appOptions.RunSettingsOnly = true;
            appOptions.RunWhenTestsFail = true;
            appOptions.ExcludeByAttribute = new[] { "GeneratedCode" };
            appOptions.IncludeTestAssembly = true;
            appOptions.ExcludeByFile = new[] { "**/Migrations/*" };
            appOptions.Enabled = true;
            appOptions.DisabledNoCoverage = true;
            appOptions.ShowEditorCoverage = true;

            appOptions.ShowCoverageInOverviewMargin = true;
            appOptions.ShowCoveredInOverviewMargin = true;
            appOptions.ShowPartiallyCoveredInOverviewMargin = true;
            appOptions.ShowUncoveredInOverviewMargin = true;

            appOptions.ShowCoverageInGlyphMargin = true;
            appOptions.ShowCoveredInGlyphMargin = true;
            appOptions.ShowPartiallyCoveredInGlyphMargin = true;
            appOptions.ShowUncoveredInGlyphMargin = true;
            
            appOptions.ShowLineCoveredHighlighting = true;
            appOptions.ShowLinePartiallyCoveredHighlighting = true;
            appOptions.ShowLineUncoveredHighlighting = true;

            appOptions.UseEnterpriseFontsAndColors = true;
            
            appOptions.Hide0Coverable = true;
        }

        public void LoadSettingsFromStorage(IAppOptions instance)
        {
            AddDefaults(instance);

            var settingsStore = EnsureStore();

            foreach (var property in appOptionsPropertyInfos)
            {
                try
                {
                    if (!settingsStore.PropertyExists(Vsix.Code, property.Name))
                    {
                        continue;
                    }

                    var strValue = settingsStore.GetString(Vsix.Code, property.Name);

                    if (string.IsNullOrWhiteSpace(strValue))
                    {
                        continue;
                    }

                    var objValue = jsonConvertService.DeserializeObject(strValue, property.PropertyType);
                    
                    property.SetValue(instance, objValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to load '{property.Name}' setting", exception);
                }
            }
        }

        public void SaveSettingsToStorage(IAppOptions appOptions)
        {
            var settingsStore = EnsureStore();

            foreach (var property in appOptionsPropertyInfos)
            {
                try
                {
                    var objValue = property.GetValue(appOptions);
                    var strValue = jsonConvertService.SerializeObject(objValue);

                    settingsStore.SetString(Vsix.Code, property.Name, strValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to save '{property.Name}' setting", exception);
                }
            }
            RaiseOptionsChanged(appOptions);
        }
    }

    internal class AppOptions : IAppOptions
    {
        public string[] Exclude { get; set; }

        public string[] ExcludeByAttribute { get; set; }

        public string[] ExcludeByFile { get; set; }

        public string[] Include { get; set; }

        public bool RunInParallel { get; set; }

        public int RunWhenTestsExceed { get; set; }

        public string ToolsDirectory { get; set; }

        public bool RunWhenTestsFail { get; set; }

        public bool RunSettingsOnly { get; set; }

        public bool CoverletConsoleGlobal { get; set; }

        public string CoverletConsoleCustomPath { get; set; }

        public bool CoverletConsoleLocal { get; set; }

        public string CoverletCollectorDirectoryPath { get; set; }

        public string OpenCoverCustomPath { get; set; }

        public string FCCSolutionOutputDirectoryName { get; set; }

        public int ThresholdForCyclomaticComplexity { get; set; }

        public int ThresholdForNPathComplexity { get; set; }

        public int ThresholdForCrapScore { get; set; }

        public bool CoverageColoursFromFontsAndColours { get; set; }

        public bool ShowCoverageInOverviewMargin { get; set; }
        
        public bool ShowCoveredInOverviewMargin { get; set; }
        
        public bool ShowUncoveredInOverviewMargin { get; set; }
        
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }
        public bool ShowDirtyInOverviewMargin { get; set; }
        public bool ShowNewInOverviewMargin { get; set; }
            
        public bool StickyCoverageTable { get; set; }

        public bool NamespacedClasses { get; set; }

        public bool HideFullyCovered { get; set; }

        public bool Hide0Coverable { get; set; }
        public bool Hide0Coverage { get; set; }

        public bool AdjacentBuildOutput { get; set; }

        public RunMsCodeCoverage RunMsCodeCoverage { get; set; }
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

        public bool Enabled { get; set; }
        public bool DisabledNoCoverage { get; set; }

        public bool IncludeTestAssembly { get; set; }

        public bool IncludeReferencedProjects { get; set; }
        public bool ShowToolWindowToolbar { get; set; }
        public string[] ExcludeAssemblies { get; set; }
        public string[] IncludeAssemblies { get; set; }
        public NamespaceQualification NamespaceQualification { get; set; }
        public OpenCoverRegister OpenCoverRegister { get; set; }
        public string OpenCoverTarget { get; set; }
        public string OpenCoverTargetArgs { get; set; }
        public bool ShowCoverageInGlyphMargin { get; set; }
        public bool ShowCoveredInGlyphMargin { get; set; }
        public bool ShowUncoveredInGlyphMargin { get; set; }
        public bool ShowPartiallyCoveredInGlyphMargin { get; set; }
        public bool ShowDirtyInGlyphMargin { get; set; }
        public bool ShowNewInGlyphMargin { get; set; }
        public bool ShowLineCoverageHighlighting { get; set; }
        public bool ShowLineCoveredHighlighting { get; set; }
        public bool ShowLineUncoveredHighlighting { get; set; }
        public bool ShowLinePartiallyCoveredHighlighting { get; set; }
        public bool ShowLineDirtyHighlighting { get; set; }
        public bool ShowLineNewHighlighting { get; set; }
        public bool ShowEditorCoverage { get; set; }
        public bool UseEnterpriseFontsAndColors { get; set; }


    }
}
