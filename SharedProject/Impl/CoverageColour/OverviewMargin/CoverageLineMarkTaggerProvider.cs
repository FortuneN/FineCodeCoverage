using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SharedProject.Core.Model;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(OverviewMarkTag))]
    [Name("FCC.CoverageLineMarkTaggerProvider")]
    [Export(typeof(ITaggerProvider))]
    internal class CoverageLineMarkTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineMarkTagger, OverviewMarkTag>
    {
        private CoverageMarginOptions coverageMarginOptions;
        [ImportingConstructor]
        public CoverageLineMarkTaggerProvider(
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

        protected override CoverageLineMarkTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines)
        {
            return new CoverageLineMarkTagger(textBuffer, lastCoverageLines, coverageMarginOptions);
        }
    }
}
