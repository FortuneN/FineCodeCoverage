using FineCodeCoverage.Options;

namespace FineCodeCoverage.Impl
{
    interface ICoverageTypeFilter
    {
        IAppOptions AppOptions { set; }
        bool Show(CoverageType coverageType);
        string TypeIdentifier { get; }
        bool Changed(ICoverageTypeFilter other);
    }
}
