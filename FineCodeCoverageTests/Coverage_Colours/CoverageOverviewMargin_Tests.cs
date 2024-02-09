using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests.Coverage_Colours
{
    internal class CoverageOverviewMargin_Tests : CoverageTypeFilter_Tests_Base<CoverageOverviewMarginFilter>
    {
        protected override Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get; } = appOptions => appOptions.ShowCoverageInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; } = appOptions => appOptions.ShowCoveredInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; } = appOptions => appOptions.ShowUncoveredInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; } = appOptions => appOptions.ShowPartiallyCoveredInOverviewMargin;
    }


}