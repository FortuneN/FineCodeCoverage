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
			var srceDir   = new DirectoryInfo(Path.GetFullPath(sourceFolder) + '\\');
			var destDir   = new DirectoryInfo(Path.GetFullPath(destinationFolder) + '\\');

			// file lists

			var srceFiles = srceDir.GetFiles("*", SearchOption.AllDirectories).Select(fi => new ComparableFile(fi, fi.FullName.Substring(srceDir.FullName.Length)));
			IEnumerable<ComparableFile> destFiles() => destDir.GetFiles("*", SearchOption.AllDirectories).Select(fi => new ComparableFile(fi, fi.FullName.Substring(destDir.FullName.Length)));

			// copy to dest

			foreach (var fileToCopy in srceFiles.Except(destFiles(), FileComparer.Singleton))
			{
				var to = new FileInfo(fileToCopy.FileInfo.FullName.Replace(srceDir.FullName, destDir.FullName));

				if (!to.Directory.Exists)
				{
					Directory.CreateDirectory(to.DirectoryName);
				}

				File.Copy(fileToCopy.FileInfo.FullName, to.FullName, true);
			}

			// delete from dest

			foreach (var fileToDelete in destFiles().Except(srceFiles, FileComparer.Singleton))
			{
				File.Delete(fileToDelete.FileInfo.FullName);
			}
		}

		private class ComparableFile : IEquatable<ComparableFile>
		{
			private readonly int hashCode;

			public FileInfo FileInfo { get; }

			public string RelativePath { get; }

			public override int GetHashCode() => hashCode;

			public bool Equals(ComparableFile other) => hashCode.Equals(other.hashCode);

			public ComparableFile(FileInfo fileInfo, string relativePath)
			{
				FileInfo = fileInfo;
				RelativePath = relativePath;
				hashCode = string.Format("{0}|{1}|{2}", RelativePath, FileInfo.Length, FileInfo.LastWriteTimeUtc.Ticks).GetHashCode();
			}
		}

		private class FileComparer : IEqualityComparer<ComparableFile>
		{
			public static FileComparer Singleton { get; } = new FileComparer();
			
			public int GetHashCode(ComparableFile file) => file.GetHashCode();

			public bool Equals(ComparableFile file, ComparableFile otherFile) => file.Equals(otherFile);
		}
	}
}
