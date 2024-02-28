namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDynamicCoverageStore
    {
        string GetSerializedCoverage(string filePath);
        void SaveSerializedCoverage(string filePath, string serialized);
    }
}
