using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Tagging.OverviewMargin
{
    internal class CoverageOverviewMarginFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "OverviewMargin";

        protected override bool Enabled(IAppOptions appOptions) => appOptions.ShowCoverageInOverviewMargin;

        protected override Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions) 
            => new Dictionary<DynamicCoverageType, bool>
            {
                { DynamicCoverageType.Covered, appOptions.ShowCoveredInOverviewMargin },
                { DynamicCoverageType.NotCovered, appOptions.ShowUncoveredInOverviewMargin },
                { DynamicCoverageType.Partial, appOptions.ShowPartiallyCoveredInOverviewMargin },
                { DynamicCoverageType.Dirty, appOptions.ShowDirtyInOverviewMargin},
                { DynamicCoverageType.NewLine, appOptions.ShowNewInOverviewMargin},
                { DynamicCoverageType.NotIncluded, appOptions.ShowNotIncludedInOverviewMargin},
            };
    }
}
