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
                var isSolutionDirectory = directory.EnumerateFiles().Any(f => f.Name.EndsWith(".sln"));
                if (isSolutionDirectory)
                {
                    provided = directory.FullName;
                    break;
                }
                directory = directory.Parent;
            }
            return provided;
        }
    }
}
