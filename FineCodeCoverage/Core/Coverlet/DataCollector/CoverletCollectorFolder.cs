using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(ICoverletCollectorFolder))]
    internal class CoverletCollectorFolder : ICoverletCollectorFolder
    {
        private readonly IZipFile zipFile;

        [ImportingConstructor]
        public CoverletCollectorFolder(IZipFile zipFile)
        {
            this.zipFile = zipFile;
        }
        public string EnsureUnzipped(string appDataFolder, ZipDetails zipDetails)
        {
            var version = zipDetails.Version;

            var coverletCollectorPath = Path.Combine(appDataFolder, "coverletCollector");
            var zipDestination = Path.Combine(coverletCollectorPath, version);
            var coverletCollectorDirectory = Directory.CreateDirectory(coverletCollectorPath);

            var unzippedDirectories = coverletCollectorDirectory.GetDirectories();
            var requiresUnzip = !unzippedDirectories.Any(d => d.Name == version);
            
            if (requiresUnzip)
            {
                Directory.CreateDirectory(zipDestination);

                zipFile.ExtractToDirectory(zipDetails.Path, zipDestination);
                
                foreach(var unzippedDirectory in unzippedDirectories)
                {
                    unzippedDirectory.TryDelete();
                }
            }
            return zipDestination;
        }
    }
}
