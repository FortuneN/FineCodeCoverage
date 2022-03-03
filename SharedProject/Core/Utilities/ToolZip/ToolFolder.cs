using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;

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

        public string EnsureUnzipped(string appDataFolder, string toolFolderName, ZipDetails zipDetails, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var version = zipDetails.Version;

            var toolFolderPath = Path.Combine(appDataFolder, toolFolderName);
            var toolDirectory = Directory.CreateDirectory(toolFolderPath);
            var zipDestination = Path.Combine(toolFolderPath, version);
            
            cancellationToken.ThrowIfCancellationRequested();
            var unzippedDirectories = toolDirectory.GetDirectories();
            var requiresUnzip = !unzippedDirectories.Any(d => d.Name == version);

            if (requiresUnzip)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var file in toolDirectory.GetFiles())
                {
                    file.TryDelete();
                }

                foreach (var unzippedDirectory in unzippedDirectories)
                {
                    unzippedDirectory.TryDelete();
                }

                Directory.CreateDirectory(zipDestination);
                zipFile.ExtractToDirectory(zipDetails.Path, zipDestination);
            }

            return zipDestination;
        }
    }
}
