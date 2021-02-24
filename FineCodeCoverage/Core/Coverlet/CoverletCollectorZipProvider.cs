using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(ICoverletCollectorZipProvider))]
    internal class CoverletCollectorZipProvider : ICoverletCollectorZipProvider
    {
        public CoverletCollectorZipProvider()
        {
            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
        internal string ExtensionDirectory { get; set; }
        public ZipDetails ProvideZip()
        {
            var zipFolder = Path.Combine(ExtensionDirectory, "Core", "Coverlet");
            var coverletCollectorZipFiles = Directory.GetFiles(zipFolder, "coverlet.collector.*.zip");
            var zipPath = coverletCollectorZipFiles.First();

            var coverletCollectorZipFile = Path.GetFileName(zipPath);
            var version = coverletCollectorZipFile.Replace("coverlet.collector.", "").Replace(".zip", "");

            return new ZipDetails { Path = zipPath, Version = version };
        }
    }
}
