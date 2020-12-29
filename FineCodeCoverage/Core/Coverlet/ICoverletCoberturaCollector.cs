using System;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverletCoberturaCollector:IDisposable {
		void CollectFrom(IOperation operation);
        string GetCollected(string testDllFile);
    }
}
