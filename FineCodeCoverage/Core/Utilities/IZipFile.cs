namespace FineCodeCoverage.Core.Utilities
{
    internal interface IZipFile
    {
        void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
    }
}
