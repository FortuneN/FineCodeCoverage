using System;
using System.ComponentModel.Composition;
using System.IO;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine
{
    [Order(2, typeof(ICoverageToolOutputFolderSolutionProvider))]
    class FccOutputExistenceCoverageToolOutputFolderSolutionProvider : ICoverageToolOutputFolderSolutionProvider
    {
        private const string fccOutputFolderName = "fcc-output";
        private readonly IFileUtil fileUtil;

        [ImportingConstructor]
        public FccOutputExistenceCoverageToolOutputFolderSolutionProvider(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
        }

        public string Provide(Func<string> solutionFolderProvider)
        {
            var solutionFolder = solutionFolderProvider();
            if (solutionFolder != null)
            {
                var provided = Path.Combine(solutionFolder, fccOutputFolderName);
                if (fileUtil.DirectoryExists(provided))
                {
                    return provided;
                }
            }
            return null;
        }
    }
}
