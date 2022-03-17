namespace FineCodeCoverage.Options
{
    internal interface IFCCCommonOptions
    {
        bool Enabled { get; }
        bool IncludeTestAssembly { get; }
        bool IncludeReferencedProjects { get; }
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
    internal interface IAppOptions : IMsCodeCoverageOptions, IFCCCommonOptions
    {
        string[] Exclude { get; }
        string[] ExcludeByAttribute { get; }
        string[] ExcludeByFile { get; }
        string[] Include { get; }
        bool RunInParallel { get; }
        int RunWhenTestsExceed { get; }
        bool RunWhenTestsFail { get; }
        bool RunSettingsOnly { get; }
        bool CoverletConsoleGlobal { get; }
        string CoverletConsoleCustomPath { get; }
        bool CoverletConsoleLocal { get; }
        string CoverletCollectorDirectoryPath { get; }
        string OpenCoverCustomPath { get; }
        string FCCSolutionOutputDirectoryName { get; }
        int ThresholdForCyclomaticComplexity { get; }
        int ThresholdForNPathComplexity { get; }
        int ThresholdForCrapScore { get; }
        bool CoverageColoursFromFontsAndColours { get; }
        bool StickyCoverageTable { get; }
        bool NamespacedClasses { get; }
        bool HideFullyCovered { get; }
        bool AdjacentBuildOutput { get; }
        
        bool MsCodeCoverage { get; set; } 
        
    }
}