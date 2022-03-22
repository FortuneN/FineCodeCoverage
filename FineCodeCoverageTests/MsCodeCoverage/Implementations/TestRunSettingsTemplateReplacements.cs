using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class TemplateReplaceResult : ITemplateReplacementResult
    {
        public string Replaced { get; set; }

        public bool ReplacedTestAdapter { get; set; }
    }

    internal class TestRunSettingsTemplateReplacements : IRunSettingsTemplateReplacements
    {
        public string Enabled { get; set; }

        public string ResultsDirectory { get; set; }

        public string TestAdapter { get; set; }

        public string ModulePathsExclude { get; set; }

        public string ModulePathsInclude { get; set; }

        public string FunctionsExclude { get; set; }

        public string FunctionsInclude { get; set; }

        public string AttributesExclude { get; set; }

        public string AttributesInclude { get; set; }

        public string SourcesExclude { get; set; }

        public string SourcesInclude { get; set; }

        public string CompanyNamesExclude { get; set; }

        public string CompanyNamesInclude { get; set; }

        public string PublicKeyTokensExclude { get; set; }

        public string PublicKeyTokensInclude { get; set; }
    }
}
