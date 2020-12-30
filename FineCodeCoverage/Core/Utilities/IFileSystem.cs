namespace FineCodeCoverage.Impl
{
    internal interface IFileSystem
    {
		bool Exists(string path);
		string GetDirectoryName(string path);
		string EnsureAbsolute(string absoluteOrRelativePath, string relativeTo);
    }
}
