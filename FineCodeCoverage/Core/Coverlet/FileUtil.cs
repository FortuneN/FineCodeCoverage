using System.ComponentModel.Composition;
using System.IO;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IFileUtil))]
    internal class FileUtil : IFileUtil
    {
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
    }
}
