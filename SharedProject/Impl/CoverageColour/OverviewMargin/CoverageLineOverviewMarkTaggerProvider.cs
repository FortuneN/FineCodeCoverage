using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(OverviewMarkTag))]
    [Name("FCC.CoverageLineOverviewMarkTaggerProvider")]
    [Export(typeof(ITaggerProvider))]
    internal class CoverageLineOverviewMarkTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineOverviewMarkTagger, OverviewMarkTag>
    {
        private CoverageMarginOptions coverageMarginOptions;
        [ImportingConstructor]
        public CoverageLineOverviewMarkTaggerProvider(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider
        ) : base(eventAggregator)
        {
            var appOptions = appOptionsProvider.Get();
            coverageMarginOptions = CoverageMarginOptions.Create(appOptions);
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            var newCoverageMarginOptions = CoverageMarginOptions.Create(appOptions);
            if (!newCoverageMarginOptions.AreEqual(coverageMarginOptions))
            {
                coverageMarginOptions = newCoverageMarginOptions;
                eventAggregator.SendMessage(new CoverageMarginOptionsChangedMessage(coverageMarginOptions));
            }
        }

        protected override CoverageLineOverviewMarkTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines)
        {
            return new CoverageLineOverviewMarkTagger(textBuffer, lastCoverageLines, coverageMarginOptions);
        }
    }
}
