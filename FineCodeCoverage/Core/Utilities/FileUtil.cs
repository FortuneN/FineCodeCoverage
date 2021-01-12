using System;
using CliWrap;
using System.IO;
using System.Linq;
using CliWrap.Buffered;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Utilities
{
	internal static class FileUtil
	{
		/// <summary>
		/// Delete all files and sub-directories from a given directory if it exists, or creates the directory if it does not exist.
		/// </summary>
		/// <param name="directory"></param>
		public static void EnsureEmptyDirectory(string directory)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(directory);
			if (directoryInfo.Exists)
			{
				foreach (FileInfo file in directoryInfo.GetFiles())
				{
					file.Delete();
				}
				foreach (DirectoryInfo subDir in directoryInfo.GetDirectories())
				{
					subDir.Delete(true);
				}
			}
			else
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}
