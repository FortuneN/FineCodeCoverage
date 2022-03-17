using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Threading;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    interface IMsCodeCoverageRunSettingsService
    {
        void Initialize(string appDataFolder,IFCCEngine fccEngine, CancellationToken cancellationToken);
        MsCodeCoverageCollectionStatus IsCollecting(ITestOperation testOperation);
        void Collect(IOperation operation, ITestOperation testOperation);
        void StopCoverage();
    }    
}
