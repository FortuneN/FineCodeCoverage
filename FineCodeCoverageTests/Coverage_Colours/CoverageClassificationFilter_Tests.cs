using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests
{
    internal class CoverageClassificationFilter_Tests : CoverageTypeFilter_Tests_Base<CoverageClassificationFilter>
    {
        protected override Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get;} = appOptions => appOptions.ShowLineCoverageHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; } = appOptions => appOptions.ShowLineCoveredHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; } = appOptions => appOptions.ShowLineUncoveredHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; } = appOptions => appOptions.ShowLinePartiallyCoveredHighlighting;
    }
}