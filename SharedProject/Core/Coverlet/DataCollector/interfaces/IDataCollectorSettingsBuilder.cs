using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface IDataCollectorSettingsBuilder
    {
        void Initialize(bool runSettingsOnly, string runSettingsPath, string generatedRunSettingsPath);

        void WithProjectDll(string projectDll);
        void WithExclude(string[] projectExclude, string runSettingsExclude);
        void WithInclude(string[] projectInclude, string runSettingsInclude);
        void WithExcludeByAttribute(string[] projectExcludeByAttribute, string runSettingsExcludeByAttribute);

        void WithExcludeByFile(string[] projectExcludeByFile, string runSettingsExcludeByFile);
        void WithIncludeTestAssembly(bool projectIncludeTestAssembly, string runSettingsIncludeTestAssembly);
        
        void WithNoLogo();
        void WithBlame();

        void WithDiagnostics(string logPath);
        void WithResultsDirectory(string resultsDirectory);
        string Build();
        void WithIncludeDirectory(string includeDirectory);
        void WithSingleHit(string singleHit);
        void WithUseSourceLink(string useSourceLink);
        void WithSkipAutoProps(string skipAutoProps);
    }
}
