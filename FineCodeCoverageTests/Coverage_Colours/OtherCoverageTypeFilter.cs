using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using System;

namespace FineCodeCoverageTests
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

        public bool Show(CoverageType coverageType)
        {
            throw new NotImplementedException();
        }
    }
}