using FineCodeCoverage.Options;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectSettingsManager))]
    internal class CoverageProjectSettingsManager : ICoverageProjectSettingsManager
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageProjectSettingsProvider coverageProjectSettingsProvider;
        private readonly IFCCSettingsFilesProvider fccSettingsFilesProvider;
        private readonly ISettingsMerger settingsMerger;

        [ImportingConstructor]
        public CoverageProjectSettingsManager(
            IAppOptionsProvider appOptionsProvider,
            ICoverageProjectSettingsProvider coverageProjectSettingsProvider,
            IFCCSettingsFilesProvider fccSettingsFilesProvider,
            ISettingsMerger settingsMerger
        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.coverageProjectSettingsProvider = coverageProjectSettingsProvider;
            this.fccSettingsFilesProvider = fccSettingsFilesProvider;
            this.settingsMerger = settingsMerger;
        }

        public async Task<IAppOptions> GetSettingsAsync(ICoverageProject coverageProject)
        {
            var projectDirectory = Path.GetDirectoryName(coverageProject.ProjectFile);
            var settingsFilesElements = fccSettingsFilesProvider.Provide(projectDirectory);
            var projectSettingsElement = await coverageProjectSettingsProvider.ProvideAsync(coverageProject);
            return settingsMerger.Merge(appOptionsProvider.Get(), settingsFilesElements, projectSettingsElement);
        }
    }

}
