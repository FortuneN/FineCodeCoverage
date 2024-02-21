using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.Tagging.Classification
{
    [ContentType(SupportedContentTypeLanguages.CSharp)]
    [ContentType(SupportedContentTypeLanguages.VisualBasic)]
    [ContentType(SupportedContentTypeLanguages.CPP)]
    [TagType(typeof(IClassificationTag))]
    [Name("FCC.CoverageLineClassificationTaggerProvider")]
    [Export(typeof(IViewTaggerProvider))]
    internal class CoverageLineClassificationTaggerProvider : IViewTaggerProvider, ILineSpanTagger<IClassificationTag>
    {
        private readonly ICoverageTypeService coverageTypeService;
        private readonly ICoverageTaggerProvider<IClassificationTag> coverageTaggerProvider;

        [ImportingConstructor]
        public CoverageLineClassificationTaggerProvider(
            ICoverageTypeService coverageTypeService,
             ICoverageTaggerProviderFactory coverageTaggerProviderFactory
        )
        {
            this.coverageTypeService = coverageTypeService;
            this.coverageTaggerProvider =  coverageTaggerProviderFactory.Create<IClassificationTag, CoverageClassificationFilter>(this);
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return coverageTaggerProvider.CreateTagger(textView,buffer) as ITagger<T>;
        }

        public TagSpan<IClassificationTag> GetTagSpan(ILineSpan lineSpan)
        {
            var ct = coverageTypeService.GetClassificationType(lineSpan.Line.CoverageType);
            return new TagSpan<IClassificationTag>(lineSpan.Span, new ClassificationTag(ct));
        }
    }
}
