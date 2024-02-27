using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDynamicCoverageStore
    {
        object GetSerializedCoverage(string filePath);
        void SaveSerializedCoverage(string filePath, object obj);
    }
}
