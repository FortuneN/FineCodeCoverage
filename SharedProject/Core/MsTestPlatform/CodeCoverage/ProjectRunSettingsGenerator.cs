using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Task = System.Threading.Tasks.Task;
using FineCodeCoverage.Engine.Model;
using System.Linq;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IProjectRunSettingsGenerator))]
    internal class ProjectRunSettingsGenerator : IProjectRunSettingsGenerator
    {
        private readonly IFileUtil fileUtil;
        private readonly IVsRunSettingsWriter vsRunSettingsWriter;
        private const string fccGeneratedRunSettingsSuffix = "fcc-mscodecoverage-generated";

        [ImportingConstructor]
        public ProjectRunSettingsGenerator(
            IFileUtil fileUtil,
            IVsRunSettingsWriter vsRunSettingsWriter
        )
        {
            this.fileUtil = fileUtil;
            this.vsRunSettingsWriter = vsRunSettingsWriter;
        }

        public async Task RemoveGeneratedProjectSettingsAsync(IEnumerable<ICoverageProject> coverageProjects)
        {
            var coverageProjectsForRemoval = coverageProjects.Where(coverageProject => IsGeneratedRunSettings(coverageProject.RunSettingsFile));
            foreach (var coverageProjectForRemoval in coverageProjectsForRemoval)
            {
                await vsRunSettingsWriter.RemoveRunSettingsFilePathAsync(coverageProjectForRemoval.Id);
            }
        }

        public async Task WriteProjectsRunSettingsAsync(IEnumerable<ICoverageProjectRunSettings> coverageProjectsRunSettings)
        {
            foreach (var coverageProjectRunSettings in coverageProjectsRunSettings)
            {
                var coverageProject = coverageProjectRunSettings.CoverageProject;
                await WriteProjectRunSettingsAsync(coverageProject.Id, GeneratedProjectRunSettingsFilePath(coverageProject), coverageProjectRunSettings.RunSettings);
            }
        }

        internal static string GeneratedProjectRunSettingsFilePath(ICoverageProject coverageProject)
        {
            return Path.Combine(coverageProject.CoverageOutputFolder, $"{coverageProject.ProjectName}-{fccGeneratedRunSettingsSuffix}.runsettings");
        }

        private async Task WriteProjectRunSettingsAsync(Guid projectGuid, string projectRunSettingsFilePath, string projectRunSettings)
        {
            if (await vsRunSettingsWriter.WriteRunSettingsFilePathAsync(projectGuid, projectRunSettingsFilePath))
            {
                fileUtil.WriteAllText(projectRunSettingsFilePath, projectRunSettings);
            }
        }

        private static bool IsGeneratedRunSettings(string runSettingsFile)
        {
            if (runSettingsFile == null)
            {
                return false;
            }

            return Path.GetFileNameWithoutExtension(runSettingsFile).EndsWith(fccGeneratedRunSettingsSuffix);
        }

    }

}
