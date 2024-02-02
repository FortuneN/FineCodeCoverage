using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests
{
    internal class GlyphFilter_Tests : CoverageTypeFilter_Tests_Base<GlyphFilter>
    {
        protected override Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get; } = appOptions => appOptions.ShowCoverageInGlyphMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; } = appOptions => appOptions.ShowCoveredInGlyphMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; } = appOptions => appOptions.ShowUncoveredInGlyphMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; } = appOptions => appOptions.ShowPartiallyCoveredInGlyphMargin;
    }


}