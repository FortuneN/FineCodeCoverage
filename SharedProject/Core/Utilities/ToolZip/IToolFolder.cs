namespace FineCodeCoverage.Core.Utilities
{
    internal interface IToolFolder
    {
        string EnsureUnzipped(string appDataFolder,string ownFolderName, ZipDetails zipDetails);
    }
}
