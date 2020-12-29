using System.IO;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class CoverletCoberturaResultsDirectoryCollector:ICoverletCoberturaResultsDirectoryCollector
    {
		private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
		private List<string> projectsCollectingToResultsDirectory = new List<string>();
		private List<string> CoberturaFiles { get; } = new List<string>();

		public CoverletCoberturaResultsDirectoryCollector(string resultsDirectory)
        {
			fileSystemWatcher.Path = resultsDirectory;
			fileSystemWatcher.Filter = "coverage.cobertura.xml";
			fileSystemWatcher.IncludeSubdirectories = true;
			fileSystemWatcher.Created += (object s, FileSystemEventArgs fseArgs) =>
			{
				CoberturaFiles.Add(fseArgs.FullPath);
			};
			fileSystemWatcher.EnableRaisingEvents = true;
		}

        public void Dispose()
        {
			fileSystemWatcher.EnableRaisingEvents = false;
			fileSystemWatcher.Dispose();
        }

        public void AddProjectCollectingToResultsDirectory(string key)
        {
			projectsCollectingToResultsDirectory.Add(key);

		}

        public string GetCollected(string testDllFile)
        {
            if(projectsCollectingToResultsDirectory.Count != CoberturaFiles.Count)
            {
				return null;
            }
			var projectIndex = projectsCollectingToResultsDirectory.IndexOf(testDllFile);
			if(projectIndex != -1)
            {
				if(CoberturaFiles.Count>=projectIndex + 1)
                {
					return CoberturaFiles[projectsCollectingToResultsDirectory.IndexOf(testDllFile)];
				}
            }
			return null;
			
        }
    }
}
