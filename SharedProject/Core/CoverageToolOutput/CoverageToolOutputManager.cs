﻿using System;
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
        private const string projectCoverageToolOutputFolderName = "coverage-tool-output";
        private string outputFolderForAllProjects;
        private List<ICoverageProject> coverageProjects;
        private readonly IOrderedEnumerable<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>> outputFolderProviders;

        [ImportingConstructor]
        public CoverageToolOutputManager(IFileUtil fileUtil, ILogger logger,[ImportMany] IEnumerable<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>> outputFolderProviders)
        {
            this.logger = logger;
            this.fileUtil = fileUtil;
            this.outputFolderProviders = outputFolderProviders.OrderBy(p => p.Metadata.Order);
        }

        public void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects)
        {
            this.coverageProjects = coverageProjects;
            DetermineOutputFolder();
            if (outputFolderForAllProjects == null)
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

        

        private void DetermineOutputFolder()
        {
            outputFolderForAllProjects = outputFolderProviders.SelectFirstNonNull(p => p.Value.Provide(coverageProjects));
            if(outputFolderForAllProjects != null)
            {
                logger.Log($"FCC output in {outputFolderForAllProjects}");
            }
        }

        public string GetReportOutputFolder()
        {
            return outputFolderForAllProjects ?? coverageProjects[0].CoverageOutputFolder;
        }
    }
}
