using System.Collections.Generic;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IDotNetConfigFinder
    {
        IEnumerable<string> GetConfigDirectories(string upFromDirectory);
    }
}
