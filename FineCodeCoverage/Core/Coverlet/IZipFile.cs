namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface IZipFile
    {
        void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
    }
}
