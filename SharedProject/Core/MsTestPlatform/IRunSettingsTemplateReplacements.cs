namespace FineCodeCoverage.Engine.MsTestPlatform
{
    internal interface IRunSettingsTemplateReplacements
    {
        string Enabled { get; }
        string ResultsDirectory { get; }
        string TestAdapter { get; }
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
