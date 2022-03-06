using FineCodeCoverage.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    interface IMsCodeCoverageRunSettingsService
    {
        void PrepareRunSettings(string solutionPath, ITestOperation testOperation);
        IList<String> GetCoverageFilesFromLastRun();        
        void Initialize(string appDataFolder);
    }    
}
