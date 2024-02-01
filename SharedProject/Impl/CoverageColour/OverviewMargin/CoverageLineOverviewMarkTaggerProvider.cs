using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
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
    internal class CoverageLineOverviewMarkTaggerProvider : ITaggerProvider, ILineSpanTagger<OverviewMarkTag>
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

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return coverageTaggerProvider.CreateTagger(buffer) as ITagger<T>;
        }

        public TagSpan<OverviewMarkTag> GetTagSpan(ILineSpan lineSpan)
        {
            var editorFormatDefinitionName = coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(lineSpan.Line.CoverageType);
            return new TagSpan<OverviewMarkTag>(lineSpan.Span, new OverviewMarkTag(editorFormatDefinitionName));
        }

    }
}
