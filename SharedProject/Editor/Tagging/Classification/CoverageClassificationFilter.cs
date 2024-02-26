using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Tagging.Classification
{
    internal class CoverageClassificationFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "Classification";

        protected override bool Enabled(IAppOptions appOptions)
        {
            return appOptions.ShowLineCoverageHighlighting;
        }

        protected override Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return new Dictionary<DynamicCoverageType, bool>()
            {
                { DynamicCoverageType.Covered, appOptions.ShowLineCoveredHighlighting },
                { DynamicCoverageType.Partial, appOptions.ShowLinePartiallyCoveredHighlighting },
                { DynamicCoverageType.NotCovered, appOptions.ShowLineUncoveredHighlighting },
                { DynamicCoverageType.Dirty, appOptions.ShowLineDirtyHighlighting },
                { DynamicCoverageType.NewLine, appOptions.ShowLineNewHighlighting },
                { DynamicCoverageType.NotIncluded, appOptions.ShowLineNotIncludedHighlighting },
            };
        }
    }


}
