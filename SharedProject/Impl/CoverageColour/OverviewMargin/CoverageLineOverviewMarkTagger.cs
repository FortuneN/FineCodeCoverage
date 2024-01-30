using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineOverviewMarkTagger : CoverageLineTaggerBase<OverviewMarkTag>
	{
        private readonly ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames;

        public CoverageLineOverviewMarkTagger(
			ITextBuffer textBuffer, 
			FileLineCoverage lastCoverageLines, 
			IEventAggregator eventAggregator,
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames,
			ICoverageTypeFilter coverageTypeFilter

        ) : base(textBuffer, lastCoverageLines,coverageTypeFilter,eventAggregator)
		{
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        protected override TagSpan<OverviewMarkTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
		{
			var editorFormatDefinitionName = coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(coverageLine.CoverageType);
			span = GetLineSnapshotSpan(coverageLine.Number, span);
			return new TagSpan<OverviewMarkTag>(span, new OverviewMarkTag(editorFormatDefinitionName));
		}
	}
}
