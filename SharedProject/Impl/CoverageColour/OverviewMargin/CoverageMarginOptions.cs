using FineCodeCoverage.Options;

namespace FineCodeCoverage.Impl
{
    internal class CoverageMarginOptions : ICoverageMarginOptions
    {
        public bool ShowCoveredInOverviewMargin { get; set; }
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }
        public bool ShowUncoveredInOverviewMargin { get; set; }

        public bool AreEqual(CoverageMarginOptions options)
        {
            return ShowUncoveredInOverviewMargin == options.ShowUncoveredInOverviewMargin &&
                ShowPartiallyCoveredInOverviewMargin == options.ShowPartiallyCoveredInOverviewMargin &&
                ShowCoveredInOverviewMargin == options.ShowCoveredInOverviewMargin;
        }

        public static CoverageMarginOptions Create(IAppOptions appOptions)
        {
            if (!appOptions.ShowCoverageInOverviewMargin)
            {
                return new CoverageMarginOptions();
            }
            return new CoverageMarginOptions
            {
                ShowCoveredInOverviewMargin = appOptions.ShowCoveredInOverviewMargin,
                ShowPartiallyCoveredInOverviewMargin = appOptions.ShowPartiallyCoveredInOverviewMargin,
                ShowUncoveredInOverviewMargin = appOptions.ShowUncoveredInOverviewMargin,
            };
        }

        public bool Show(CoverageType coverageType)
        {
            var shouldShow = false;
            switch (coverageType)
            {
                case CoverageType.Covered:
                    shouldShow = ShowCoveredInOverviewMargin;
                    break;
                case CoverageType.NotCovered:
                    shouldShow = ShowUncoveredInOverviewMargin;
                    break;
                case CoverageType.Partial:
                    shouldShow = ShowPartiallyCoveredInOverviewMargin;
                    break;
            }
            return shouldShow;
        }
    }

}
