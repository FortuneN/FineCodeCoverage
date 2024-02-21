using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Classification;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageTypeService
    {
        IClassificationType GetClassificationType(DynamicCoverageType coverageType);
    }
}
