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
        private readonly IClassificationTypeRegistryService classificationTypeRegistryService;
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        [ImportingConstructor]
        public CoverageLineClassificationTaggerProvider(
            IEventAggregator eventAggregator,
            IClassificationTypeRegistryService classificationTypeRegistryService,
             ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
            ) : base(eventAggregator)
        {
            this.classificationTypeRegistryService = classificationTypeRegistryService;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        protected override CoverageLineClassificationTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator)
        {
            return new CoverageLineClassificationTagger(
                textBuffer, lastCoverageLines, eventAggregator, classificationTypeRegistryService, coverageLineCoverageTypeInfoHelper);
        }
    }
    internal class CoverageLineClassificationTagger : CoverageLineTaggerBase<IClassificationTag>
    {
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;
        private readonly IClassificationTypeRegistryService classificationTypeRegistryService;

        public CoverageLineClassificationTagger(
            ITextBuffer textBuffer,
            FileLineCoverage lastCoverageLines,
            IEventAggregator eventAggregator,
            IClassificationTypeRegistryService classificationTypeRegistryService,
             ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
        ) : base(textBuffer, lastCoverageLines, eventAggregator)
        {
            this.classificationTypeRegistryService = classificationTypeRegistryService;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        protected override TagSpan<IClassificationTag> GetTagSpan(Line coverageLine, SnapshotSpan span)
        {
            span = GetLineSnapshotSpan(coverageLine.Number, span);
            var coverageType = coverageLineCoverageTypeInfoHelper.GetInfo(coverageLine).CoverageType;
            var ct = classificationTypeRegistryService.GetClassificationType(
                GetClassificationTypeName(coverageType)
            ); 
            return new TagSpan<IClassificationTag>(span, new ClassificationTag(ct));
        }

        private SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
        {
            var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

            var startPoint = line.Start;
            var endPoint = line.End;

            return new SnapshotSpan(startPoint, endPoint);
        }

        private string GetClassificationTypeName(CoverageType coverageType)
        {
            var classificationTypeName = CoveredEditorFormatDefinition.ResourceName;
            switch (coverageType)
            {
                case CoverageType.NotCovered:
                    classificationTypeName = NotCoveredEditorFormatDefinition.ResourceName;
                    break;
                case CoverageType.Partial:
                    classificationTypeName = PartiallyCoveredEditorFormatDefinition.ResourceName;
                    break;
            }
            return classificationTypeName;
        }
    }
}
