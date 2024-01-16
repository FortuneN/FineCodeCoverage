using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace FineCodeCoverage.Core.Utilities
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IFileUtil))]
    internal class FileUtil : IFileUtil
    {
        public string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void TryDeleteDirectory(string directory)
        {
            new DirectoryInfo(directory).TryDelete();
        }

        public bool DirectoryExists(string directory)
        {
            return Directory.Exists(directory);
        }

        public string EnsureAbsolute(string directory, string possiblyRelativeTo)
        {
            if (!Path.IsPathRooted(directory))
            {
                directory =  Path.GetFullPath(Path.Combine(possiblyRelativeTo, directory));
            }
            return directory;
        }

        public string FileDirectoryPath(string filePath)
        {
            return new FileInfo(filePath).Directory.FullName;
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void TryEmptyDirectory(string directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            if (directoryInfo.Exists)
            {
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.TryDelete();
                }
                foreach (DirectoryInfo subDir in directoryInfo.GetDirectories())
                {
                    subDir.TryDelete(true);
                }
            }
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }

        public void Copy(string source, string destination)
        {
            File.Copy(source, destination);
        }

        public string DirectoryParentPath(string directoryPath)
        {
            var parentDirectory = new DirectoryInfo(directoryPath).Parent;
            if(parentDirectory == null)
            {
                return null;
            }
            return parentDirectory.FullName;
        }

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }
    }
}
