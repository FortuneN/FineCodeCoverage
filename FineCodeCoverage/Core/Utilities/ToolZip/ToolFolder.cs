using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IToolFolder))]
    internal class ToolFolder : IToolFolder
    {
        private readonly IZipFile zipFile;

        [ImportingConstructor]
        public ToolFolder(IZipFile zipFile)
        {
            this.zipFile = zipFile;
        }
        public string EnsureUnzipped(string appDataFolder, string toolFolderName, ZipDetails zipDetails)
        {
            var version = zipDetails.Version;

            var toolFolderPath = Path.Combine(appDataFolder, toolFolderName);
            var zipDestination = Path.Combine(toolFolderPath, version);
            var toolDirectory = Directory.CreateDirectory(toolFolderPath);

            var unzippedDirectories = toolDirectory.GetDirectories();
            var requiresUnzip = !unzippedDirectories.Any(d => d.Name == version);

            if (requiresUnzip)
            {
                Directory.CreateDirectory(zipDestination);

                zipFile.ExtractToDirectory(zipDetails.Path, zipDestination);
                foreach(var file in toolDirectory.GetFiles())
                {
                    file.TryDelete();
                }
                foreach (var unzippedDirectory in unzippedDirectories)
                {
                    unzippedDirectory.TryDelete();
                }
            }
            return zipDestination;
        }
    }
}
