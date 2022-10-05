using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTagger : CoverageLineTaggerBase<CoverageLineGlyphTag>, IListener<RefreshCoverageGlyphsMessage>
	{
		public CoverageLineGlyphTagger(ITextBuffer textBuffer, Dictionary<string, List<CoverageLine>> lastCoverageLines) : base(textBuffer, lastCoverageLines)
		{
		}

        public void Handle(RefreshCoverageGlyphsMessage message)
        {
			RaiseTagsChanged();
        }

        protected override TagSpan<CoverageLineGlyphTag> GetTagSpan(CoverageLine coverageLine, SnapshotSpan span)
        {
			return new TagSpan<CoverageLineGlyphTag>(span, new CoverageLineGlyphTag(coverageLine));
        }
    }
}