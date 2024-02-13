using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.Tagging.OverviewMargin
{
    [ContentType(SupportedContentTypeLanguages.CSharp)]
    [ContentType(SupportedContentTypeLanguages.VisualBasic)]
    [ContentType(SupportedContentTypeLanguages.CPP)]
    [TagType(typeof(OverviewMarkTag))]
    [Name("FCC.CoverageLineOverviewMarkTaggerProvider")]
    [Export(typeof(IViewTaggerProvider))]
    internal class CoverageLineOverviewMarkTaggerProvider : IViewTaggerProvider, ILineSpanTagger<OverviewMarkTag>
    {
        private readonly ICoverageTaggerProvider<OverviewMarkTag> coverageTaggerProvider;
        private readonly ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames;

        [ImportingConstructor]
        public CoverageLineOverviewMarkTaggerProvider(
            ICoverageTaggerProviderFactory coverageTaggerProviderFactory,
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames,
            ILineSpanLogic lineSpanLogic
        )
        {
            coverageTaggerProvider = coverageTaggerProviderFactory.Create<OverviewMarkTag, CoverageOverviewMarginFilter>(this);
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView,ITextBuffer buffer) where T : ITag
        {
            return coverageTaggerProvider.CreateTagger(textView, buffer) as ITagger<T>;
        }

        public TagSpan<OverviewMarkTag> GetTagSpan(ILineSpan lineSpan)
        {
            var coverageType = DirtyCoverageTypeMapper.GetClean(lineSpan.Line.CoverageType);
            var editorFormatDefinitionName = coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(coverageType);
            return new TagSpan<OverviewMarkTag>(lineSpan.Span, new OverviewMarkTag(editorFormatDefinitionName));
        }

    }
}
