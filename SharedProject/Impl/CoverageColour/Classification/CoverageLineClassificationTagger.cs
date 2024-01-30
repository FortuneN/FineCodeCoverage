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
            FileLineCoverage lastCoverageLines,
            IEventAggregator eventAggregator,
            ICoverageTypeService coverageTypeService,
            ICoverageTypeFilter coverageTypeFilter) : base(textBuffer, lastCoverageLines, coverageTypeFilter, eventAggregator)
        {
            this.coverageTypeService = coverageTypeService;
        }

        protected override TagSpan<IClassificationTag> GetTagSpan(Line coverageLine, SnapshotSpan span)
        {
            span = GetLineSnapshotSpan(coverageLine.Number, span);
            var ct = coverageTypeService.GetClassificationType(coverageLine.CoverageType);
            return new TagSpan<IClassificationTag>(span, new ClassificationTag(ct));
        }

    }

}
