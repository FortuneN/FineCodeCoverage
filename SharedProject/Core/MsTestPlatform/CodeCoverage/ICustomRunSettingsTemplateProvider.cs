namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    public class CustomRunSettingsTemplateDetails
    {
        public string Template { get; set; }
        public string Path { get; set; }
    }
    internal interface ICustomRunSettingsTemplateProvider
    {
        CustomRunSettingsTemplateDetails Provide(string projectDirectory, string solutionDirectory);
    }
}
