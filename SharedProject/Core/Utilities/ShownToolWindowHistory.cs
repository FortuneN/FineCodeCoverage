using FineCodeCoverage.Engine;
using System.ComponentModel.Composition;
using System.IO;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IShownToolWindowHistory))]
    internal class ShownToolWindowHistory : IShownToolWindowHistory
    {
        private readonly IFCCEngine fccEngine;
        private readonly IFileUtil fileUtil;
        private bool hasShownToolWindow;
        private bool checkedFileExists;

        [ImportingConstructor]
        public ShownToolWindowHistory(IFCCEngine fccEngine, IFileUtil fileUtil)
        {
            this.fccEngine = fccEngine;
            this.fileUtil = fileUtil;
        }
        private string ShownToolWindowFilePath => Path.Combine(fccEngine.AppDataFolderPath, "outputWindowInitialized");
        public bool HasShownToolWindow
        {
            get
            {
                if (!hasShownToolWindow && !checkedFileExists)
                {
                    hasShownToolWindow = fileUtil.Exists(ShownToolWindowFilePath);
                    checkedFileExists = true;
                }
                return hasShownToolWindow;
            }
        }

        public void ShowedToolWindow()
        {
            if (!hasShownToolWindow)
            {
                hasShownToolWindow = true;
                fileUtil.WriteAllText(ShownToolWindowFilePath, string.Empty);
            }
        }
    }
}
