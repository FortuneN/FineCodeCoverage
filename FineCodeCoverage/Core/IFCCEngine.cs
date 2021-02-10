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
        void Initialize();
        void StopCoverage();
        void ReloadCoverage(Func<System.Threading.Tasks.Task<List<CoverageProject>>> coverageRequestCallback);
        string[] GetSourceFiles(string assemblyName, string qualifiedClassName);
        IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber);
        void ClearUI();
    }

}