using System;
using System.Collections.Generic;
using System.Windows;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Engine
{
    internal interface IFCCEngine
    {
        event UpdateMarginTagsDelegate UpdateMarginTags;
        event UpdateOutputWindowDelegate UpdateOutputWindow;
        string AppDataFolderPath { get; }
        void Initialize(IInitializeStatusProvider initializeStatusProvider);
        void StopCoverage();
        void ReloadCoverage(Func<System.Threading.Tasks.Task<List<ICoverageProject>>> coverageRequestCallback);

        DpiScale Dpi { get; set; }

        void ClearUI();
        List<CoverageLine> CoverageLines { get; }
        FontDetails EnvironmentFontDetails { get; set; }

        void ReadyForReport();
    }

}