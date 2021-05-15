using System;
using System.ComponentModel.Composition;
using System.IO;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IFileUtil))]
    internal class FileUtil : IFileUtil
    {
        public string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
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

        public string ParentDirectoryPath(string filePath)
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
       
    }
}
