using System.ComponentModel.Composition;
using System.IO;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    [Export(typeof(ICustomRunSettingsTemplateProvider))]
    internal class CustomRunSettingsTemplateProvider : ICustomRunSettingsTemplateProvider
    {
        private const string TemplateName = "fcc-ms-runsettings-template.xml";

        public CustomRunSettingsTemplateDetails Provide(string projectDirectory, string solutionDirectory)
        {
            var runSettingsTemplate = GetTemplateIfExistsInDirectory(projectDirectory);
            return runSettingsTemplate ?? GetTemplateIfExistsInDirectory(solutionDirectory);
        }

        private CustomRunSettingsTemplateDetails GetTemplateIfExistsInDirectory(string directory)
        {
            if (directory == null)
            {
                return null;
            }

            var templatePath = Path.Combine(directory, TemplateName);
            if (File.Exists(templatePath))
            {
                return new CustomRunSettingsTemplateDetails
                {
                    Template = File.ReadAllText(templatePath),
                    Path = templatePath
                };
            }
            return null;
        }
    }
}
