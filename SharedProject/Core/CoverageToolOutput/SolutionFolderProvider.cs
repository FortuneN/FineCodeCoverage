using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ISolutionFolderProvider))]
    class SolutionFolderProvider : ISolutionFolderProvider
    {
        public string Provide(string projectFile)
        {
            string provided = null;
            var directory = new FileInfo(projectFile).Directory;
            while (directory != null)
            {
                var isSolutionDirectory = directory.EnumerateFiles().Any(IsSolutionFile);
                if (isSolutionDirectory)
                {
                    provided = directory.FullName;
                    break;
                }
                directory = directory.Parent;
            }
            return provided;
        }

        private bool IsSolutionFile(FileInfo fileInfo)
        {
            return fileInfo.Name.EndsWith(".sln") || fileInfo.Name.EndsWith(".slnx");
        }
    }
}
