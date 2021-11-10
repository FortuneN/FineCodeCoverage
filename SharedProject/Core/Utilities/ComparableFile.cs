using System;
using System.IO;

namespace FineCodeCoverage.Core.Utilities
{
	internal class ComparableFile : IEquatable<ComparableFile>
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
}