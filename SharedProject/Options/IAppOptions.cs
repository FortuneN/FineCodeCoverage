namespace FineCodeCoverage.Options
{
    internal interface IFCCCommonOptions
    {
        bool Enabled { get; set; }
        bool IncludeTestAssembly { get; set; }
        bool IncludeReferencedProjects { get; set; }
    }

    internal interface IMsCodeCoverageIncludesExcludesOptions
    {
        string[] ModulePathsExclude { get; set; }
        string[] ModulePathsInclude { get; set; }
        string[] CompanyNamesExclude { get; set; }
        string[] CompanyNamesInclude { get; set; }
        string[] PublicKeyTokensExclude { get; set; }
        string[] PublicKeyTokensInclude { get; set; }
        string[] SourcesExclude { get; set; }
        string[] SourcesInclude { get; set; }
        string[] AttributesExclude { get; set; }
        string[] AttributesInclude { get; set; }
        string[] FunctionsInclude { get; set; }
        string[] FunctionsExclude { get; set; }
    }
    internal interface IMsCodeCoverageOptions : IMsCodeCoverageIncludesExcludesOptions, IFCCCommonOptions { }

    internal enum RunMsCodeCoverage { No, IfInRunSettings, Yes }

    internal interface IAppOptions : IMsCodeCoverageOptions, IFCCCommonOptions
    {
        string[] Exclude { get; set; }
        string[] ExcludeByAttribute { get; set; }
        string[] ExcludeByFile { get; set; }
        string[] Include { get; set; }
        bool RunInParallel { get; set; }
        int RunWhenTestsExceed { get; set; }
        string ToolsDirectory { get; set; }
        bool RunWhenTestsFail { get; set; }
        bool RunSettingsOnly { get; set; }
        bool CoverletConsoleGlobal { get; set; }
        string CoverletConsoleCustomPath { get; set; }
        bool CoverletConsoleLocal { get; set; }
        string CoverletCollectorDirectoryPath { get; set; }
        string OpenCoverCustomPath { get; set; }
        string FCCSolutionOutputDirectoryName { get; set; }
        int ThresholdForCyclomaticComplexity { get; set; }
        int ThresholdForNPathComplexity { get; set; }
        int ThresholdForCrapScore { get; set; }
        bool CoverageColoursFromFontsAndColours { get; set; }
        bool ShowCoverageInOverviewMargin { get; set; }
        bool ShowCoveredInOverviewMargin { get; set; }
        bool ShowUncoveredInOverviewMargin { get; set; }
        bool ShowPartiallyCoveredInOverviewMargin { get; set; }
        bool StickyCoverageTable { get; set; }
        bool NamespacedClasses { get; set; }
        bool HideFullyCovered { get; set; }
        bool AdjacentBuildOutput { get; set; }
        RunMsCodeCoverage RunMsCodeCoverage { get; set; } 
        bool ShowToolWindowToolbar { get; set; }
    }
}