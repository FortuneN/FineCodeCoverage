using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
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
