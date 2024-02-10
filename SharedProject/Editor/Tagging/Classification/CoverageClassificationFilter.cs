using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
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

        protected override Dictionary<CoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return new Dictionary<CoverageType, bool>()
            {
                { CoverageType.Covered, appOptions.ShowLineCoveredHighlighting },
                { CoverageType.Partial, appOptions.ShowLinePartiallyCoveredHighlighting },
                { CoverageType.NotCovered, appOptions.ShowLineUncoveredHighlighting },
            };
        }
    }


}
