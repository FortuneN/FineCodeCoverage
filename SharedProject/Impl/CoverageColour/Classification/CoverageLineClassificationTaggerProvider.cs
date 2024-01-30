using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace SharedProject.Impl.CoverageColour.Classification
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
             IAppOptionsProvider appOptionsProvider
            ) : base(eventAggregator, appOptionsProvider)
        {
            this.coverageTypeService = coverageTypeService;
        }

        protected override CoverageLineClassificationTagger CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator, ICoverageTypeFilter coverageTypeFilter)
        {
            return new CoverageLineClassificationTagger(
                textBuffer, lastCoverageLines, eventAggregator, coverageTypeService,coverageTypeFilter);
        }
    }
}
