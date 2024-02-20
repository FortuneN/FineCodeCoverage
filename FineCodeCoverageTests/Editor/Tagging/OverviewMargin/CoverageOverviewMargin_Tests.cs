using FineCodeCoverage.Editor.Tagging.OverviewMargin;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.Editor.Tagging.CoverageTypeFilter;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests.Editor.Tagging.OverviewMargin
{
    internal class CoverageOverviewMargin_Tests : CoverageTypeFilter_Tests_Base<CoverageOverviewMarginFilter>
    {
        protected override Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get; } = appOptions => appOptions.ShowCoverageInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; } = appOptions => appOptions.ShowCoveredInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; } = appOptions => appOptions.ShowUncoveredInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; } = appOptions => appOptions.ShowPartiallyCoveredInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowDirtyExpression => appOptions => appOptions.ShowDirtyInOverviewMargin;

        protected override Expression<Func<IAppOptions, bool>> ShowNewExpression => appOptions => appOptions.ShowNewInOverviewMargin;
    }


}