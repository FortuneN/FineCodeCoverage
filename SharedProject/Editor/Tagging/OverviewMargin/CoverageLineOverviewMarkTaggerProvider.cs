using System.ComponentModel.Composition;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Editor.Tagging.OverviewMargin
{
    [ContentType(CSharpCoverageContentType.ContentType)]
    [ContentType(VBCoverageContentType.ContentType)]
    [ContentType(CPPCoverageContentType.ContentType)]
    [ContentType(BlazorCoverageContentType.ContentType)]
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
            this.coverageTaggerProvider = coverageTaggerProviderFactory.Create<OverviewMarkTag, CoverageOverviewMarginFilter>(this);
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
            => this.coverageTaggerProvider.CreateTagger(textView, buffer) as ITagger<T>;

        public TagSpan<OverviewMarkTag> GetTagSpan(ILineSpan lineSpan)
        {
            string editorFormatDefinitionName = this.coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(
                lineSpan.Line.CoverageType);
            return new TagSpan<OverviewMarkTag>(lineSpan.Span, new OverviewMarkTag(editorFormatDefinitionName));
        }
    }
}