using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Task = System.Threading.Tasks.Task;
using FineCodeCoverage.Engine.Model;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using System.Xml.Linq;
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IProjectRunSettingsGenerator))]
    internal class ProjectRunSettingsGenerator : IProjectRunSettingsGenerator
    {
        private readonly IFileUtil fileUtil;
        private readonly IVsRunSettingsWriter vsRunSettingsWriter;
        private readonly ILogger logger;
        private const string fccGeneratedRunSettingsSuffix = "fcc-mscodecoverage-generated";

        [ImportingConstructor]
        public ProjectRunSettingsGenerator(
            IFileUtil fileUtil,
            IVsRunSettingsWriter vsRunSettingsWriter,
            ILogger logger
        )
        {
            this.fileUtil = fileUtil;
            this.vsRunSettingsWriter = vsRunSettingsWriter;
            this.logger = logger;
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
                    return WriteProjectRunSettingsAsync(coverageProject.Id, coverageProject.ProjectName, projectRunSettingsFilePath, coverageProjectRunSettings.RunSettings);
                })
            );
            
        }

        internal static string GeneratedProjectRunSettingsFilePath(ICoverageProject coverageProject)
        {
            return Path.Combine(coverageProject.CoverageOutputFolder, $"{coverageProject.ProjectName}-{fccGeneratedRunSettingsSuffix}.runsettings");
        }

        private async Task WriteProjectRunSettingsAsync(Guid projectGuid,string projectName, string projectRunSettingsFilePath, string projectRunSettings)
        {
            var ok = await vsRunSettingsWriter.WriteRunSettingsFilePathAsync(projectGuid, projectRunSettingsFilePath);
            if (ok)
            {
                projectRunSettings = XDocument.Parse(projectRunSettings).FormatXml();
                fileUtil.WriteAllText(projectRunSettingsFilePath, projectRunSettings);
                logger.Log($"runsettings written to {projectRunSettingsFilePath}", projectRunSettings);
            }
            else
            {
                logger.Log($"Issue writing runsettings for {projectName}");
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
