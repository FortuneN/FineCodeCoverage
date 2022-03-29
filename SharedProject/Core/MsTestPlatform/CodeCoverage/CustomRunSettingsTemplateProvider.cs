using FineCodeCoverage.Core.Utilities;
using System.ComponentModel.Composition;
using System.IO;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(ICustomRunSettingsTemplateProvider))]
    internal class CustomRunSettingsTemplateProvider : ICustomRunSettingsTemplateProvider
    {
        private const string TemplateName = "fcc-ms-runsettings-template.xml";
        private readonly IFileUtil fileUtil;

        [ImportingConstructor]
        public CustomRunSettingsTemplateProvider(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
        }

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
            if (fileUtil.Exists(templatePath))
            {
                return new CustomRunSettingsTemplateDetails
                {
                    Template = fileUtil.ReadAllText(templatePath),
                    Path = templatePath
                };
            }
            return null;
        }
    }
}
