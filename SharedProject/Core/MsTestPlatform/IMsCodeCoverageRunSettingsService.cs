using FineCodeCoverage.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    interface IMsCodeCoverageRunSettingsService
    {
        void PrepareRunSettings(string solutionPath, ITestOperation testOperation);
        IList<String> GetCoverageFilesFromLastRun();        
        void Initialize(string appDataFolder, CancellationToken cancellationToken);
    }    
}
