using System;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine
{
    internal interface IFCCEngine
    {
        event UpdateMarginTagsDelegate UpdateMarginTags;
        event UpdateOutputWindowDelegate UpdateOutputWindow;
        string AppDataFolder { get; }
        void Initialize(IServiceProvider serviceProvider);
        void StopCoverage();
        void TryReloadCoverage(Func<IAppOptions, System.Threading.Tasks.Task<ReloadCoverageRequest>> coverageRequestCallback);
        string[] GetSourceFiles(string assemblyName, string qualifiedClassName);
        IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber);
    }

}