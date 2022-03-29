using System;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;

namespace FineCodeCoverage.Engine
{
    internal interface IFCCEngine
    {
        string AppDataFolderPath { get; }
        void Initialize(IInitializeStatusProvider initializeStatusProvider, System.Threading.CancellationToken cancellationToken);
        void StopCoverage();
        void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback);
        void RunAndProcessReport(string[] coberturaFiles,Action cleanUp = null);
        void ClearUI();
    }

}