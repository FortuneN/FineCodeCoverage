using System.ComponentModel.Composition;
using System.IO.Compression;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IZipFile))]
    internal class ZipFileWrapper : IZipFile
    {
        public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
        }
    }
}
