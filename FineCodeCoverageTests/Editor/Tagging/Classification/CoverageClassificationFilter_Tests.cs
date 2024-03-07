using FineCodeCoverage.Editor.Tagging.Classification;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.Editor.Tagging.CoverageTypeFilter;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests.Editor.Tagging.Classification
{
    internal class CoverageClassificationFilter_Tests : CoverageTypeFilter_Tests_Base<CoverageClassificationFilter>
    {
        protected override Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get; } = appOptions => appOptions.ShowLineCoverageHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; } = appOptions => appOptions.ShowLineCoveredHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; } = appOptions => appOptions.ShowLineUncoveredHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; } = appOptions => appOptions.ShowLinePartiallyCoveredHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowDirtyExpression => appOptions => appOptions.ShowLineDirtyHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowNewExpression => appOptions => appOptions.ShowLineNewHighlighting;

        protected override Expression<Func<IAppOptions, bool>> ShowNotIncludedExpression => appOptions => appOptions.ShowLineNotIncludedHighlighting;
    }
}