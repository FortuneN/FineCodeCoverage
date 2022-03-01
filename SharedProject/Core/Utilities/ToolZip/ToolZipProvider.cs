using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IToolZipProvider))]
    internal class ToolZipProvider : IToolZipProvider
    {
        internal const string ZippedToolsDirectoryName = "ZippedTools";
        internal string ExtensionDirectory { get; set; }
        public ToolZipProvider()
        {
            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public ZipDetails ProvideZip(string zipPrefix)
        {
            var zipFolder = Path.Combine(ExtensionDirectory, ZippedToolsDirectoryName);
            var matchingZipFiles = Directory.GetFiles(zipFolder, $"{zipPrefix}.*.zip");
            var zipPath = matchingZipFiles.First();

            var zipFileName = Path.GetFileName(zipPath);
            var version = zipFileName.Replace($"{zipPrefix}.", "").Replace(".zip", "");

            return new ZipDetails { Path = zipPath, Version = version };
        }
    }
}
