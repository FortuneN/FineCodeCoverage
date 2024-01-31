using FineCodeCoverage.Options;

namespace FineCodeCoverage.Impl
{
    interface ICoverageTypeFilter
    {
        void Initialize(IAppOptions appOptions);
        bool Disabled { get; }
        bool Show(CoverageType coverageType);
        string TypeIdentifier { get; }
        bool Changed(ICoverageTypeFilter other);
    }
}
