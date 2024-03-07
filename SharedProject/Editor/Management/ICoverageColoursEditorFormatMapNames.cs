using FineCodeCoverage.Editor.DynamicCoverage;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageColoursEditorFormatMapNames
    {
        string GetEditorFormatDefinitionName(DynamicCoverageType coverageType);
    }
}
