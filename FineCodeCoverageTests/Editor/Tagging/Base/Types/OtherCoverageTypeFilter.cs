using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;
using System;

namespace FineCodeCoverageTests.Editor.Tagging.Base.Types
{
    internal class OtherCoverageTypeFilter : ICoverageTypeFilter
    {
        public bool Disabled => throw new NotImplementedException();

        public string TypeIdentifier => "Other";

        public bool Changed(ICoverageTypeFilter other)
        {
            throw new NotImplementedException();
        }

        public void Initialize(IAppOptions appOptions)
        {
            throw new NotImplementedException();
        }

        public bool Show(DynamicCoverageType coverageType)
        {
            throw new NotImplementedException();
        }
    }
}