using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Task = System.Threading.Tasks.Task;
using FineCodeCoverage.Engine.Model;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using System.Xml.Linq;

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

        public Task RemoveGeneratedProjectSettingsAsync(IEnumerable<ICoverageProject> coverageProjects)
        {
            return Task.WhenAll(
                coverageProjects
                .Where(coverageProject => IsGeneratedRunSettings(coverageProject.RunSettingsFile))
                .Select(coverageProjectForRemoval => vsRunSettingsWriter.RemoveRunSettingsFilePathAsync(coverageProjectForRemoval.Id))
            );
        }

        public Task WriteProjectsRunSettingsAsync(IEnumerable<ICoverageProjectRunSettings> coverageProjectsRunSettings)
        {
            return Task.WhenAll(
                coverageProjectsRunSettings.Select(coverageProjectRunSettings =>
                {
                    var coverageProject = coverageProjectRunSettings.CoverageProject;
                    var projectRunSettingsFilePath = GeneratedProjectRunSettingsFilePath(coverageProject);
                    return WriteProjectRunSettingsAsync(coverageProject.Id, projectRunSettingsFilePath, coverageProjectRunSettings.RunSettings);
                })
            );
            
        }

        internal static string GeneratedProjectRunSettingsFilePath(ICoverageProject coverageProject)
        {
            return Path.Combine(coverageProject.CoverageOutputFolder, $"{coverageProject.ProjectName}-{fccGeneratedRunSettingsSuffix}.runsettings");
        }

        private async Task WriteProjectRunSettingsAsync(Guid projectGuid, string projectRunSettingsFilePath, string projectRunSettings)
        {

            if (await vsRunSettingsWriter.WriteRunSettingsFilePathAsync(projectGuid, projectRunSettingsFilePath))
            {
                projectRunSettings = XDocument.Parse(projectRunSettings).FormatXml();
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
