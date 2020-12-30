using System.IO;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IFileSystem))]
    internal class FileSystem : IFileSystem
    {
		private static bool IsValidDriveChar(char value)
		{
			return ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z'));
		}
		private static bool IsDirectorySeparator(char c)
		{
			return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
		}
		//https://referencesource.microsoft.com/#mscorlib/system/io/pathinternal.cs,4010b50c6b59181e,references
		private static bool IsPartiallyQualified(string path)
		{
			if (path.Length < 2)
			{
				// It isn't fixed, it must be relative.  There is no way to specify a fixed
				// path with one character (or less).
				return true;
			}

			if (IsDirectorySeparator(path[0]))
			{
				// There is no valid way to specify a relative path with two initial slashes or
				// \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
				return !(path[1] == '?' || IsDirectorySeparator(path[1]));
			}

			// The only way to specify a fixed path that doesn't begin with two slashes
			// is the drive, colon, slash format- i.e. C:\
			return !((path.Length >= 3)
				&& (path[1] == Path.VolumeSeparatorChar)
				&& IsDirectorySeparator(path[2])
				// To match old behavior we'll check the drive character for validity as the path is technically
				// not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
				&& IsValidDriveChar(path[0]));
		}
		public string EnsureAbsolute(string absoluteOrRelativePath, string relativeTo)
        {
            if (IsPartiallyQualified(absoluteOrRelativePath))
            {
				return Path.Combine(relativeTo, absoluteOrRelativePath);
            }
			return absoluteOrRelativePath;
		}

		public bool Exists(string path)
        {
			return File.Exists(path);
        }

        public string GetDirectoryName(string path)
        {
			return Path.GetDirectoryName(path);
        }
    }
}
