using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Output;
using SharedProject.Core.CoverageToolOutput;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ICoverageToolOutputManager))]
    internal class CoverageToolOutputManager : ICoverageToolOutputManager
    {
        private readonly ILogger logger;
        private readonly IEventAggregator eventAggregator;
        private readonly IFileUtil fileUtil;
        private string outputFolderForAllProjects;
        private List<ICoverageProject> coverageProjects;
        private readonly IOrderedEnumerable<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>> outputFolderProviders;

        [ImportingConstructor]
        public CoverageToolOutputManager(
            IFileUtil fileUtil, 
            ILogger logger,[ImportMany] IEnumerable<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>> outputFolderProviders,
            IEventAggregator eventAggregator
            )
        {
            this.logger = logger;
            this.eventAggregator = eventAggregator;
            this.fileUtil = fileUtil;
            this.outputFolderProviders = outputFolderProviders.OrderBy(p => p.Metadata.Order);
        }

        public void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects)
        {
            eventAggregator.SendMessage(new OutdatedOutputMessage());
            this.coverageProjects = coverageProjects;
            DetermineOutputFolderForAllProjects();
            if (outputFolderForAllProjects == null)
            {
                foreach(var coverageProject in coverageProjects)
                {
                    coverageProject.CoverageOutputFolder = coverageProject.DefaultCoverageOutputFolder;
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

        

        private void DetermineOutputFolderForAllProjects()
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
