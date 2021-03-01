namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletCollectorFolder
    {
        string EnsureUnzipped(string appDataFolder, ZipDetails zipDetails);
    }
}
