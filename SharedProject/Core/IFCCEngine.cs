using System;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;

namespace FineCodeCoverage.Engine
{
    internal interface IFCCEngine
    {
        event UpdateMarginTagsDelegate UpdateMarginTags;
        string AppDataFolderPath { get; }
        void Initialize(IInitializeStatusProvider initializeStatusProvider);
        void StopCoverage();
        void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback);

        void ClearUI();
        List<CoverageLine> CoverageLines { get; }
    }

}