using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Engine.Utilities;

namespace FineCodeCoverage.Engine.FileSynchronization
{
    internal interface IFileSynchronizationUtil
    {
		List<string> Synchronize(string sourceFolder, string destinationFolder, string fineCodeCoverageFolderName);
	}

	[Export(typeof(IFileSynchronizationUtil))]
	internal class FileSynchronizationUtil: IFileSynchronizationUtil
	{
		public List<string> Synchronize(string sourceFolder, string destinationFolder,string fineCodeCoverageFolderName)
		{
			var logs = new List<string>();
			var srceDir = new DirectoryInfo(Path.GetFullPath(sourceFolder) + '\\');
			var destDir = new DirectoryInfo(Path.GetFullPath(destinationFolder) + '\\');

			// file lists
			var sourceFileInfos = srceDir.GetFiles().Concat(srceDir.GetDirectories().Where(d => d.Name != fineCodeCoverageFolderName).SelectMany(d => d.GetFiles("*", SearchOption.AllDirectories)));
			var srceFiles = sourceFileInfos.AsParallel().Select(fi => new ComparableFile(fi, fi.FullName.Substring(srceDir.FullName.Length)));
			ParallelQuery<ComparableFile> destFiles() => destDir.GetFiles("*", SearchOption.AllDirectories).AsParallel().Select(fi => new ComparableFile(fi, fi.FullName.Substring(destDir.FullName.Length)));

			// copy to dest

			foreach (var fileToCopy in srceFiles.Except(destFiles(), FileComparer.Singleton))
			{
				var to = new FileInfo(fileToCopy.FileInfo.FullName.Replace(srceDir.FullName, destDir.FullName));

				if (!to.Directory.Exists)
				{
					try
					{
						Directory.CreateDirectory(to.DirectoryName);
						logs.Add($"Create : {to.DirectoryName}");
					}
					catch (Exception exception)
					{
						logs.Add($"Create : {to.DirectoryName} : {exception.Message}");
						continue;
					}
				}

				try
				{
					File.Copy(fileToCopy.FileInfo.FullName, to.FullName, true);
					logs.Add($"Copy : {fileToCopy.FileInfo.FullName} -> {to.FullName}");
				}
				catch (Exception exception)
				{
					logs.Add($"Copy : {fileToCopy.FileInfo.FullName} -> {to.FullName} : {exception.Message}");
				}
			}

			// delete from dest

			foreach (var fileToDelete in destFiles().Except(srceFiles, FileComparer.Singleton))
			{
				try
				{
					File.Delete(fileToDelete.FileInfo.FullName);
					logs.Add($"Delete : {fileToDelete.FileInfo.FullName}");
				}
				catch (Exception exception)
				{
					logs.Add($"Delete : {fileToDelete.FileInfo.FullName} : {exception.Message}");
				}
			}

			// return

			return logs;
		}
	}
}