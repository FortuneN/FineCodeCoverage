using System;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverletCoberturaResultsDirectoryCollector:IDisposable {
        void AddProjectCollectingToResultsDirectory(string key);
        string GetCollected(string testDllFile);
    }
}
