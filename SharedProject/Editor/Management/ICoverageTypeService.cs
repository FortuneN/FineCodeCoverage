using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Classification;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageTypeService
    {
        IClassificationType GetClassificationType(CoverageType coverageType);
    }
}
