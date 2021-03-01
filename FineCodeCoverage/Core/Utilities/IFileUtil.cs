namespace FineCodeCoverage.Core.Utilities
{
    internal interface IFileUtil
    {
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
    }
}
