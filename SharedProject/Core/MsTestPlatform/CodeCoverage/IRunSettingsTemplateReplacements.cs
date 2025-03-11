namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    // string values for string.Replace of templated values - e.g %fcc_modulepaths_exclude%
    internal interface IRunSettingsTemplateReplacements
    {
        string Enabled { get; }
        string ResultsDirectory { get; }
        string TestAdapter { get; }

        // the following are xml fragments as strings
        // e.g <ModulePath>path1</ModulePath><ModulePath>path2</ModulePath>
        string ModulePathsExclude { get; }
        string ModulePathsInclude { get; }
        string FunctionsExclude { get; }
        string FunctionsInclude { get; }
        string AttributesExclude { get; }
        string AttributesInclude { get; }
        string SourcesExclude { get; }
        string SourcesInclude { get; }
        string CompanyNamesExclude { get; }
        string CompanyNamesInclude { get; }
        string PublicKeyTokensExclude { get; }
        string PublicKeyTokensInclude { get; }
    }
}
