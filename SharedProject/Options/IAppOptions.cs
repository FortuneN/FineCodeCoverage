namespace FineCodeCoverage.Options
{
    public interface IAppOptions
    {
        bool Enabled { get; }
        string[] Exclude { get; }
        string[] ExcludeByAttribute { get; }
        string[] ExcludeByFile { get; }
        string[] Include { get; }
        bool IncludeTestAssembly { get; }
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
    }
}