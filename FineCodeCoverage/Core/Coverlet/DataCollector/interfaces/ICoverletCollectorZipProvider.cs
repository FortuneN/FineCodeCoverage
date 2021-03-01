namespace FineCodeCoverage.Engine.Coverlet
{
    internal class ZipDetails
    {
        public string Path { get; set; }
        public string Version { get; set; }
    }

    internal interface ICoverletCollectorZipProvider
    {
        ZipDetails ProvideZip();
    }
}
