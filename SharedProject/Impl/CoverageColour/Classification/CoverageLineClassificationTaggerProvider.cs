using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace SharedProject.Impl.CoverageColour.Classification
{

    [ContentType("code")]
    [TagType(typeof(IClassificationTag))]
    [Name("FCC.CoverageLineClassificationTaggerProvider")]
    [Export(typeof(ITaggerProvider))]
    internal class CoverageLineClassificationTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineClassificationTagger, IClassificationTag>
    {
        private readonly ICoverageTypeService coverageTypeService;
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        [ImportingConstructor]
        public CoverageLineClassificationTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageTypeService coverageTypeService,
             ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
            ) : base(eventAggregator)
        {
            this.coverageTypeService = coverageTypeService;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        protected override CoverageLineClassificationTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator)
        {
            return new CoverageLineClassificationTagger(
                textBuffer, lastCoverageLines, eventAggregator, coverageTypeService, coverageLineCoverageTypeInfoHelper);
        }
    }
    internal class CoverageLineClassificationTagger : CoverageLineTaggerBase<IClassificationTag>
    {
        private readonly ICoverageTypeService coverageTypeService;
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        public CoverageLineClassificationTagger(
            ITextBuffer textBuffer,
            FileLineCoverage lastCoverageLines,
            IEventAggregator eventAggregator,
            ICoverageTypeService coverageTypeService,
            ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
        ) : base(textBuffer, lastCoverageLines, eventAggregator)
        {
            this.coverageTypeService = coverageTypeService;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        protected override TagSpan<IClassificationTag> GetTagSpan(Line coverageLine, SnapshotSpan span)
        {
            span = GetLineSnapshotSpan(coverageLine.Number, span);
            var coverageType = coverageLineCoverageTypeInfoHelper.GetInfo(coverageLine).CoverageType;
            var ct = coverageTypeService.GetClassificationType(coverageType);
            return new TagSpan<IClassificationTag>(span, new ClassificationTag(ct));
        }

        private SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
        {
            var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

            var startPoint = line.Start;
            var endPoint = line.End;

            return new SnapshotSpan(startPoint, endPoint);
        }
    }
}
