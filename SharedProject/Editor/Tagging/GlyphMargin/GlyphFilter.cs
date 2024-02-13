using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    internal class GlyphFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "Glyph";

        protected override bool Enabled(IAppOptions appOptions)
        {
            return appOptions.ShowCoverageInGlyphMargin;
        }

        protected override Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return new Dictionary<DynamicCoverageType, bool>()
            {
                { DynamicCoverageType.Covered, appOptions.ShowCoveredInGlyphMargin },
                { DynamicCoverageType.Partial, appOptions.ShowPartiallyCoveredInGlyphMargin },
                { DynamicCoverageType.NotCovered, appOptions.ShowUncoveredInGlyphMargin },
                { DynamicCoverageType.CoveredDirty, appOptions.ShowCoveredInGlyphMargin },
                { DynamicCoverageType.PartialDirty, appOptions.ShowPartiallyCoveredInGlyphMargin },
                { DynamicCoverageType.NotCoveredDirty, appOptions.ShowUncoveredInGlyphMargin },
                { DynamicCoverageType.NewLine, true },
            };
        }
    }
}
