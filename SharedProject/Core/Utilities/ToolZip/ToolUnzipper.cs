using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IToolUnzipper))]
    internal class ToolUnzipper : IToolUnzipper
    {
        private readonly IToolZipProvider toolZipProvider;
        private readonly IToolFolder toolFolder;

        [ImportingConstructor]
        public ToolUnzipper(
            IToolZipProvider toolZipProvider,
            IToolFolder toolFolder
            )
        {
            this.toolZipProvider = toolZipProvider;
            this.toolFolder = toolFolder;
        }
        public string EnsureUnzipped(string appDataFolder, string ownFolderName, string zipPrefix, CancellationToken cancellationToken)
        {
            return toolFolder.EnsureUnzipped(appDataFolder, ownFolderName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
        }
    }
}
