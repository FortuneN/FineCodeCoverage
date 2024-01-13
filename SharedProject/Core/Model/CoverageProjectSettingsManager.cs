using FineCodeCoverage.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
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
            var merged = settingsMerger.Merge(appOptionsProvider.Get(), settingsFilesElements, projectSettingsElement);
            AddCommonAssemblyExcludesIncludes(merged);
            return merged;
        }

        private void AddCommonAssemblyExcludesIncludes(IAppOptions appOptions)
        {
            var (newOldStyleExclude,newMsExclude) = AddCommon(appOptions.Exclude, appOptions.ModulePathsExclude, appOptions.ExcludeAssemblies);
            var (newOldStyleInclude,newMsInclude) = AddCommon(appOptions.Include, appOptions.ModulePathsInclude, appOptions.IncludeAssemblies);
            appOptions.Exclude = newOldStyleExclude;
            appOptions.Include = newOldStyleInclude;
            appOptions.ModulePathsExclude = newMsExclude;
            appOptions.ModulePathsInclude = newMsInclude;
        }
        
        private (string[] newOldStyle,string[] newMs) AddCommon(string[] oldStyle,string[] ms, string[] common )
        {
            if(common == null)
            {
                return(oldStyle,ms);
            }
            var newMs = ListFromExisting(ms);
            var newOldStyle = ListFromExisting(oldStyle);
            
            common.ToList().ForEach(assemblyFileName =>
            {
                var msModulePath = $".*\\{assemblyFileName}.dll$";
                newMs.Add(msModulePath);
                var old = $"[{assemblyFileName}]*";
                newOldStyle.Add(old);
            });
            
            return (newOldStyle.ToArray(), newMs.ToArray());
        }

        private List<string> ListFromExisting(string[] existing)
        {
            return new List<string>(existing ?? new string[0]);
        }
    }

}
