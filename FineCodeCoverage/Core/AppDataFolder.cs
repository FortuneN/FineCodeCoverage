using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(IAppDataFolder))]
    internal class AppDataFolder : IAppDataFolder
    {
        public string DirectoryPath { get; private set; }

        public void Initialize()
        {
            CreateAppDataFolder();

            CleanupLegacyFolders();

        }

        private void CreateAppDataFolder()
        {
            DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
            Directory.CreateDirectory(DirectoryPath);
        }

        private void CleanupLegacyFolders()
        {
            Directory
            .GetDirectories(DirectoryPath, "*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var name = Path.GetFileName(path);

                if (name.Contains("__"))
                {
                    return true;
                }

                if (Guid.TryParse(name, out var _))
                {
                    return true;
                }

                return false;
            })
            .ToList()
            .ForEach(path =>
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch
                {
                    // ignore
                }
            });
        }

    }

}