namespace FineCodeCoverage.Core.Utilities
{
    internal class ZipDetails
    {
        public string Path { get; set; }
        public string Version { get; set; }
    }
    internal interface IToolZipProvider
    {
        ZipDetails ProvideZip(string zipPrefix);
    }
}
