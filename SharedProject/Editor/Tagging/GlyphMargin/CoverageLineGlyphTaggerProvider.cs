using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.DynamicCoverage;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    [ContentType(SupportedContentTypeLanguages.CSharp)]
    [ContentType(SupportedContentTypeLanguages.VisualBasic)]
    [ContentType(SupportedContentTypeLanguages.CPP)]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
	[Export(typeof(IViewTaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : IViewTaggerProvider, ILineSpanTagger<CoverageLineGlyphTag>
    {
        private readonly ICoverageTaggerProvider<CoverageLineGlyphTag> coverageTaggerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageColoursProvider coverageColoursProvider;

        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageColoursProvider coverageColoursProvider,
            ICoverageTaggerProviderFactory coverageTaggerProviderFactory
        )
        {
            coverageTaggerProvider = coverageTaggerProviderFactory.Create<CoverageLineGlyphTag,GlyphFilter>(this);
            this.eventAggregator = eventAggregator;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView,ITextBuffer buffer) where T : ITag
        {
            var coverageTagger =  coverageTaggerProvider.CreateTagger(textView,buffer);
            if (coverageTagger == null) return null;
            return new CoverageLineGlyphTagger(eventAggregator, coverageTagger) as ITagger<T>;
        }

        public TagSpan<CoverageLineGlyphTag> GetTagSpan(ILineSpan lineSpan)
        {
            var coverageLine = lineSpan.Line;
            var coverageColours = coverageColoursProvider.GetCoverageColours();
            System.Windows.Media.Color colour = Colors.Pink;
            if(coverageLine.CoverageType != DynamicCoverageType.NewLine)
            {
                if (DirtyCoverageTypeMapper.IsDirty(coverageLine.CoverageType))
                {
                    colour = Colors.Brown;
                }
                else
                {
                    var coverageType = DirtyCoverageTypeMapper.GetClean(coverageLine.CoverageType);
                    colour = coverageColours.GetColour(coverageType).Background;
                }
                
            }
            
            return new TagSpan<CoverageLineGlyphTag>(lineSpan.Span, new CoverageLineGlyphTag(colour));
        }
    }

    
}