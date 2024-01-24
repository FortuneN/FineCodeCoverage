using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTagger : CoverageLineTaggerBase<CoverageLineGlyphTag>
	{
		public CoverageLineGlyphTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines) : base(textBuffer, lastCoverageLines)
		{
		}

        protected override TagSpan<CoverageLineGlyphTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
        {
			return new TagSpan<CoverageLineGlyphTag>(span, new CoverageLineGlyphTag(coverageLine));
        }
    }
}