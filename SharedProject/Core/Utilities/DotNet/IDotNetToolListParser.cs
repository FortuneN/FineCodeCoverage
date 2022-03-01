using System.Collections.Generic;

namespace FineCodeCoverage.Core.Utilities
{
    // ignoring Manifest as not needed
    internal class DotNetTool
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string Commands { get; set; }
    }

    internal interface IDotNetToolListParser
    {
        List<DotNetTool> Parse(string output);
    }
}