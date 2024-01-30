using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class GlyphTagFilter : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "Glyph";

        protected override bool Enabled(IAppOptions appOptions)
        {
            return appOptions.ShowCoverageInGlyphMargin;
        }

        protected override Dictionary<CoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return new Dictionary<CoverageType, bool>()
            {
                { CoverageType.Covered, appOptions.ShowCoveredInGlyphMargin },
                { CoverageType.Partial, appOptions.ShowPartiallyCoveredInGlyphMargin },
                { CoverageType.NotCovered, appOptions.ShowUncoveredInGlyphMargin },
            };
        }
    }
}
