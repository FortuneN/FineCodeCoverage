using System;
using System.IO;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IDirectoryFilePoller
    {
        Task<FileInfo> PollAsync(string directory, string fileName, int timeoutMs, Func<FileInfo[], FileInfo> selector, SearchOption searchOption);
    }
}
