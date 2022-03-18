namespace FineCodeCoverage.Core.Utilities
{
    internal interface IFileUtil
    {
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        string CreateTempDirectory();
        bool DirectoryExists(string directory);
        void TryEmptyDirectory(string directory);
        string EnsureAbsolute(string directory, string possiblyRelativeTo);
        string ParentDirectoryPath(string filePath);
        void TryDeleteDirectory(string directory);
        bool Exists(string filePath);
    }
}
