using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDynamicCoverageStore
    {
        List<IDynamicLine> GetSerializedCoverage(string filePath);
        void SaveSerializedCoverage(string filePath, IEnumerable<IDynamicLine> dynamicLines);
    }
}
