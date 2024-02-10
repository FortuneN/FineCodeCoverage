using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Tagging.Base
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
