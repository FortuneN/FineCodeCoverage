using System.Collections.Generic;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    internal class GlyphFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "Glyph";

        protected override bool Enabled(IAppOptions appOptions) => appOptions.ShowCoverageInGlyphMargin;

        protected override Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions)
            => new Dictionary<DynamicCoverageType, bool>
            {
                { DynamicCoverageType.Covered, appOptions.ShowCoveredInGlyphMargin },
                { DynamicCoverageType.Partial, appOptions.ShowPartiallyCoveredInGlyphMargin },
                { DynamicCoverageType.NotCovered, appOptions.ShowUncoveredInGlyphMargin },
                { DynamicCoverageType.Dirty, appOptions.ShowDirtyInGlyphMargin },
                { DynamicCoverageType.NewLine, appOptions.ShowNewInGlyphMargin },
                { DynamicCoverageType.NotIncluded, appOptions.ShowNotIncludedInGlyphMargin },
            };
    }
}
