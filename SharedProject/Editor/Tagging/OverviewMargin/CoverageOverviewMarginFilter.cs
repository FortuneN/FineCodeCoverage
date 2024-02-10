using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class CoverageOverviewMarginFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "OverviewMargin";

        protected override bool Enabled(IAppOptions appOptions)
        {
            return appOptions.ShowCoverageInOverviewMargin;
        }

        protected override Dictionary<CoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return new Dictionary<CoverageType, bool>
            {
                { CoverageType.Covered, appOptions.ShowCoveredInOverviewMargin },
                { CoverageType.NotCovered, appOptions.ShowUncoveredInOverviewMargin },
                { CoverageType.Partial, appOptions.ShowPartiallyCoveredInOverviewMargin }
            };
        }
    }
}
