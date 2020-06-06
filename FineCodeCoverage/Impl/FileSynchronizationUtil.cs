using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal static class FileSynchronizationUtil
    {
        public static void Synchronize(string sourceFolder, string destinationFolder)
        {
            var srceDir = new DirectoryInfo(Path.GetFullPath(sourceFolder) + '\\');
            var destDir = new DirectoryInfo(Path.GetFullPath(destinationFolder) + '\\');

            // take snapshot of files

            var srceFiles = srceDir.GetFiles("*.*", SearchOption.AllDirectories).Select(fi => new ComparableFile(fi, fi.FullName.Substring(srceDir.FullName.Length)));
            var destFiles = destDir.GetFiles("*.*", SearchOption.AllDirectories).Select(fi => new ComparableFile(fi, fi.FullName.Substring(destDir.FullName.Length)));

            // copy to dest

            foreach (var fileToCopy in srceFiles.Except(destFiles, FileComparer.Singleton))
            {
                var to = new FileInfo(fileToCopy.FileInfo.FullName.Replace(srceDir.FullName, destDir.FullName));

                if (!to.Directory.Exists)
                {
                    Directory.CreateDirectory(to.DirectoryName);
                }

                File.Copy(fileToCopy.FileInfo.FullName, to.FullName, true);
            }

            // delete from dest

            foreach (var fileToDelete in destFiles.Except(srceFiles, FileComparer.Singleton))
            {
                File.Delete(fileToDelete.FileInfo.FullName);
            }
        }

        private class ComparableFile : IEquatable<ComparableFile>
        {
            public FileInfo FileInfo { get; }

            public string RelativePath { get; }

            public ComparableFile(FileInfo fileInfo, string relativePath)
            {
                FileInfo = fileInfo;
                RelativePath = relativePath;
            }

            public bool Equals(ComparableFile other)
            {
                return RelativePath == other.RelativePath && FileInfo.Length == other.FileInfo.Length;
            }
        }

        private class FileComparer : IEqualityComparer<ComparableFile>
        {
            public static FileComparer Singleton { get; } = new FileComparer();

            public bool Equals(ComparableFile file, ComparableFile otherFile)
            {
                return file.RelativePath == otherFile.RelativePath &&
                       file.FileInfo.Name == otherFile.FileInfo.Name &&
                       file.FileInfo.Length == otherFile.FileInfo.Length;
            }

            public int GetHashCode(ComparableFile file)
            {
                return string.Format("{0}{1}", file.RelativePath, file.FileInfo.Length).GetHashCode();
            }
        }
    }
}
