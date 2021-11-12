using System.Collections.Generic;

namespace FineCodeCoverage.Core.Utilities
{
	internal class FileComparer : IEqualityComparer<ComparableFile>
	{
		public static FileComparer Singleton { get; } = new FileComparer();

		public int GetHashCode(ComparableFile file) => file.GetHashCode();

		public bool Equals(ComparableFile file, ComparableFile otherFile) => file.Equals(otherFile);
	}
}