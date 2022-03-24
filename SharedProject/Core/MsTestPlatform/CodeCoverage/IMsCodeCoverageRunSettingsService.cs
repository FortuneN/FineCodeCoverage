using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    interface IMsCodeCoverageRunSettingsService
    {
        void Initialize(string appDataFolder,IFCCEngine fccEngine, CancellationToken cancellationToken);
        Task<MsCodeCoverageCollectionStatus> IsCollectingAsync(ITestOperation testOperation);
        Task CollectAsync(IOperation operation, ITestOperation testOperation);
        void StopCoverage();
        Task TestExecutionNotFinishedAsync();
    }    
}
