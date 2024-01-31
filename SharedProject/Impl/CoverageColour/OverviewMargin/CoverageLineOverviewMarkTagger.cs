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
			IFileLineCoverage lastCoverageLines, 
			IEventAggregator eventAggregator,
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames,
			ICoverageTypeFilter coverageTypeFilter,
			ILineSpanLogic lineSpanLogic

        ) : base(textBuffer, lastCoverageLines,coverageTypeFilter,eventAggregator, lineSpanLogic)
		{
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }

        protected override TagSpan<OverviewMarkTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
		{
			var editorFormatDefinitionName = coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(coverageLine.CoverageType);
			return new TagSpan<OverviewMarkTag>(span, new OverviewMarkTag(editorFormatDefinitionName));
		}
	}
}
