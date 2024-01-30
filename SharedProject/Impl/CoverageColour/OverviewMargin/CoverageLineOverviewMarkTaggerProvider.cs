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
    internal class CoverageLineOverviewMarkTaggerProvider : 
        CoverageLineTaggerProviderBase<CoverageLineOverviewMarkTagger, OverviewMarkTag,CoverageOverviewMarginFilter>
    {
        private readonly ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames;

        [ImportingConstructor]
        public CoverageLineOverviewMarkTaggerProvider(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames
        ) : base(eventAggregator,appOptionsProvider)
        {
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        protected override CoverageLineOverviewMarkTagger CreateTagger(
            ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator, ICoverageTypeFilter coverageTypeFilter)
        {
            return new CoverageLineOverviewMarkTagger(
                textBuffer, lastCoverageLines, eventAggregator, coverageColoursEditorFormatMapNames, coverageTypeFilter);
        }
    }
}
