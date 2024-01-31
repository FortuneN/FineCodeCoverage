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
    [TagType(typeof(IClassificationTag))]
    [Name("FCC.CoverageLineClassificationTaggerProvider")]
    [Export(typeof(ITaggerProvider))]
    internal class CoverageLineClassificationTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineClassificationTagger, IClassificationTag, CoverageClassificationFilter>
    {
        private readonly ICoverageTypeService coverageTypeService;

        [ImportingConstructor]
        public CoverageLineClassificationTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageTypeService coverageTypeService,
             IAppOptionsProvider appOptionsProvider,
             ILineSpanLogic lineSpanLogic
            ) : base(eventAggregator, appOptionsProvider,lineSpanLogic)
        {
            this.coverageTypeService = coverageTypeService;
        }

        protected override CoverageLineClassificationTagger CreateCoverageTagger(
            ITextBuffer textBuffer, IFileLineCoverage lastCoverageLines, IEventAggregator eventAggregator, CoverageClassificationFilter coverageTypeFilter,ILineSpanLogic lineSpanLogic)
        {
            return new CoverageLineClassificationTagger(
                textBuffer, lastCoverageLines, eventAggregator, coverageTypeService,coverageTypeFilter,lineSpanLogic);
        }
    }
}
