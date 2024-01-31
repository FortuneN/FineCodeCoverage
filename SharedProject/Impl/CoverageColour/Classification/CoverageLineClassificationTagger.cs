using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;

namespace FineCodeCoverage.Impl
{
    internal class CoverageLineClassificationTagger : CoverageLineTaggerBase<IClassificationTag>
    {
        private readonly ICoverageTypeService coverageTypeService;

        public CoverageLineClassificationTagger(
            ITextBuffer textBuffer,
            IFileLineCoverage lastCoverageLines,
            IEventAggregator eventAggregator,
            ICoverageTypeService coverageTypeService,
            ICoverageTypeFilter coverageTypeFilter,
            ILineSpanLogic lineSpanLogic
            ) : base(textBuffer, lastCoverageLines, coverageTypeFilter, eventAggregator, lineSpanLogic)
        {
            this.coverageTypeService = coverageTypeService;
        }

        protected override TagSpan<IClassificationTag> GetTagSpan(Line coverageLine, SnapshotSpan span)
        {
            var ct = coverageTypeService.GetClassificationType(coverageLine.CoverageType);
            return new TagSpan<IClassificationTag>(span, new ClassificationTag(ct));
        }

    }

}
