using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ICoverageToolOutputManager))]
    internal class CoverageToolOutputManager : ICoverageToolOutputManager
    {
        private readonly ILogger logger;
        private readonly IFileUtil fileUtil;
        private const string unifiedHtmlFileName = "index.html";
        private const string unifiedXmlFileName = "Cobertura.xml";
        private const string processedHtmlFileName = "index-processed.html";
        private const string projectCoverageToolOutputFolderName = "coverage-tool-output";
        private string outputFolderForAllProjects;
        private List<ICoverageProject> coverageProjects;

        [ImportingConstructor]
        public CoverageToolOutputManager(IFileUtil fileUtil, ILogger logger)
        {
            this.logger = logger;
            this.fileUtil = fileUtil;
        }

        public void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects)
        {
            this.coverageProjects = coverageProjects;
            DetermineOutputFolderForAllProjects();
            if(outputFolderForAllProjects == null)
            {
                foreach(var coverageProject in coverageProjects)
                {
                    coverageProject.CoverageOutputFolder = Path.Combine(coverageProject.FCCOutputFolder, projectCoverageToolOutputFolderName);
                }
            }
            else
            {
                fileUtil.TryEmptyDirectory(outputFolderForAllProjects);
                foreach (var coverageProject in coverageProjects)
                {
                    coverageProject.CoverageOutputFolder = Path.Combine(outputFolderForAllProjects, coverageProject.ProjectName);
                }
            }
        }

        public void SetReportOutput(string unifiedHtml, string processedReport, string unifiedXml)
        {
            var outputFolder = outputFolderForAllProjects ?? coverageProjects[0].CoverageOutputFolder;

            fileUtil.WriteAllText(Path.Combine(outputFolder, unifiedHtmlFileName), unifiedHtml);
            fileUtil.WriteAllText(Path.Combine(outputFolder, processedHtmlFileName), processedReport);
            fileUtil.WriteAllText(Path.Combine(outputFolder, unifiedXmlFileName), unifiedXml);
        }

        private void DetermineOutputFolderForAllProjects()
        {
            outputFolderForAllProjects = null;
            var coverageProjectWithAllProjectsCoverageOutputFolder = coverageProjects.FirstOrDefault(cp => cp.AllProjectsCoverageOutputFolder != null);
            if(coverageProjectWithAllProjectsCoverageOutputFolder != null)
            {
                var allProjectsCoverageOutputFolder = fileUtil.EnsureAbsolute(
                    coverageProjectWithAllProjectsCoverageOutputFolder.AllProjectsCoverageOutputFolder,
                    fileUtil.ParentDirectoryPath(coverageProjectWithAllProjectsCoverageOutputFolder.ProjectFile)
                );
         
                outputFolderForAllProjects = allProjectsCoverageOutputFolder;
                logger.Log($"Outputting coverage files to - {outputFolderForAllProjects}");
                return;
            }
            
            
            logger.Log($"Outputting coverage files in project output folder");

        }
    }
}
