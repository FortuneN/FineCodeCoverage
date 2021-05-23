using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IDotNetConfigFinder))]
    internal class DotNetConfigFinder : IDotNetConfigFinder
    {
        private bool DirectoryContainsConfig(DirectoryInfo directoryInfo)
        {
            return directoryInfo.GetDirectories().Any(dir => dir.Name == ".config");
        }
        public IEnumerable<string> GetConfigDirectories(string upFromDirectory)
        {
            var currentDirectory = new DirectoryInfo(upFromDirectory);
            while (true)
            {
                if (DirectoryContainsConfig(currentDirectory))
                {
                    yield return currentDirectory.FullName;
                }
                var parentDirectory = currentDirectory.Parent;
                if (parentDirectory != null)
                {
                    currentDirectory = parentDirectory;
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
