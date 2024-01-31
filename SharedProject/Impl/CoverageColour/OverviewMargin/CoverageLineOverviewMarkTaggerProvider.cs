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
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames,
            ILineSpanLogic lineSpanLogic
        ) : base(eventAggregator,appOptionsProvider, lineSpanLogic)
        {
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        protected override CoverageLineOverviewMarkTagger CreateCoverageTagger(
            ITextBuffer textBuffer, IFileLineCoverage lastCoverageLines, IEventAggregator eventAggregator, CoverageOverviewMarginFilter coverageTypeFilter,ILineSpanLogic lineSpanLogic)
        {
            return new CoverageLineOverviewMarkTagger(
                textBuffer, lastCoverageLines, eventAggregator, coverageColoursEditorFormatMapNames, coverageTypeFilter,lineSpanLogic);
        }
    }
}
