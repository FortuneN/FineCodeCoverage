using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine
{

    [Export(typeof(IAppDataFolder))]
    internal class AppDataFolder : IAppDataFolder
    {
        private readonly ILogger logger;
        private readonly IEnvironmentVariable environmentVariable;
        internal const string fccDebugCleanInstallEnvironmentVariable = "FCCDebugCleanInstall";

        [ImportingConstructor]
        public AppDataFolder(ILogger logger,IEnvironmentVariable environmentVariable)
        {
            this.logger = logger;
            this.environmentVariable = environmentVariable;
        }
        public string DirectoryPath { get; private set; }

        public void Initialize()
        {
            CreateAppDataFolder();

            CleanupLegacyFolders();

        }

        private void CreateAppDataFolder()
        {
            DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
            if (environmentVariable.Get(fccDebugCleanInstallEnvironmentVariable) != null)
            {
                logger.Log("FCCDebugCleanInstall");
                if (Directory.Exists(DirectoryPath))
                {
                    try
                    {
                        Directory.Delete(DirectoryPath, true);
                        logger.Log("Deleted app data folder");
                    }
                    catch (Exception exc)
                    {
                        logger.Log("Error deleting app data folder", exc);
                    }
                }
                else
                {
                    logger.Log("App data folder does not exist");
                }
            }
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