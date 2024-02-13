using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    interface ICoverageTypeFilter
    {
        void Initialize(IAppOptions appOptions);
        bool Disabled { get; }
        bool Show(DynamicCoverageType coverageType);
        string TypeIdentifier { get; }
        bool Changed(ICoverageTypeFilter other);
    }
}
