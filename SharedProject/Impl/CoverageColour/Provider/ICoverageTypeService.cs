using Microsoft.VisualStudio.Text.Classification;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageTypeService
    {
        IClassificationType GetClassificationType(CoverageType coverageType);
    }
}
