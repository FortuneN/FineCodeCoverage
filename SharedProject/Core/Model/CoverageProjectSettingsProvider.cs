using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectSettingsProvider))]
    internal class CoverageProjectSettingsProvider : ICoverageProjectSettingsProvider
    {
        private readonly IVsBuildFCCSettingsProvider vsBuildFCCSettingsProvider;

        [ImportingConstructor]
        public CoverageProjectSettingsProvider(
            IVsBuildFCCSettingsProvider vsBuildFCCSettingsProvider
        )
        {
            this.vsBuildFCCSettingsProvider = vsBuildFCCSettingsProvider;
        }
        public async Task<XElement> ProvideAsync(ICoverageProject coverageProject)
        {
            var settingsElement = ProjectSettingsElementFromFCCLabelledPropertyGroup(coverageProject) ?? await vsBuildFCCSettingsProvider.GetSettingsAsync(coverageProject.Id);
            return settingsElement;
        }

        private XElement ProjectSettingsElementFromFCCLabelledPropertyGroup(ICoverageProject coverageProject)
        {
            /*
            <PropertyGroup Label="FineCodeCoverage">
                ...
            </PropertyGroup>
            */
            return coverageProject.ProjectFileXElement.XPathSelectElement($"/PropertyGroup[@Label='{Vsix.Code}']");
        }
    }

}
