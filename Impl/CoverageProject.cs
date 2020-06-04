using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class CoverageProject
    {
        public string FolderPath { get; set; }

        public List<CoverageSourceFile> SourceFiles { get; } = new List<CoverageSourceFile>();
    }
}
