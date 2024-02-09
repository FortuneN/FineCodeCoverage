using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using System;

namespace FineCodeCoverageTests.Coverage_Colours
{
    internal class DummyCoverageTypeFilterInitializedEventArgs
    {
        public DummyCoverageTypeFilterInitializedEventArgs(DummyCoverageTypeFilter dummyCoverageTypeFilter)
        {
            DummyCoverageTypeFilter = dummyCoverageTypeFilter;
        }

        public DummyCoverageTypeFilter DummyCoverageTypeFilter { get; }
    }

    internal class DummyCoverageTypeFilter : ICoverageTypeFilter
    {
        public static event EventHandler<DummyCoverageTypeFilterInitializedEventArgs> Initialized;

        public bool Disabled { get; set; }

        public string TypeIdentifier => "Dummy";

        public bool Show(CoverageType coverageType)
        {
            throw new NotImplementedException();
        }

        public Func<DummyCoverageTypeFilter, bool> ChangedFunc { get; set; }
        public bool Changed(ICoverageTypeFilter other)
        {
            return ChangedFunc(other as DummyCoverageTypeFilter);
        }
        public IAppOptions AppOptions { get; private set; }
        public void Initialize(IAppOptions appOptions)
        {
            AppOptions = appOptions;
            Initialized?.Invoke(this, new DummyCoverageTypeFilterInitializedEventArgs(this));
        }
    }
}