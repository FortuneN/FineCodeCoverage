using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IDirectoryFilePoller))]
    internal class DirectoryFilePoller : IDirectoryFilePoller
    {
        private string directory;
        private string fileName;
        private SearchOption searchOption;
        private Func<FileInfo[], FileInfo> selector;
        public Task<FileInfo> PollAsync(string directory, string fileName, int timeoutMs,  Func<FileInfo[],FileInfo> selector = null, SearchOption searchOption = SearchOption.AllDirectories)
        {
            this.selector = selector ?? (files => files[0]);
            this.directory = directory;
            this.fileName = fileName;
            this.searchOption = searchOption;
            
            return Task.Run(() =>
            {
                FileInfo file = null;
                var startTime = new DateTime();
                while (true)
                {
                    file = FindFile();
                    if (file != null)
                    {
                        break;
                    }
                    if (timeoutMs != 0)
                    {
                        var endTime = new DateTime();
                        var elapsed = endTime - startTime;
                        if (elapsed.TotalMilliseconds > timeoutMs)
                        {
                            break;
                        }
                    }
                }
                return file;
            });
            
        }

        private FileInfo FindFile()
        {
            var coverageOutputDirectory = new DirectoryInfo(directory);
            var foundFiles = coverageOutputDirectory.GetFiles(fileName, searchOption);
            switch (foundFiles.Length)
            {
                case 0:
                    return null;
                case 1:
                    return foundFiles[0];
                default:
                    return selector(foundFiles);

            }
            
        }
    }
}
