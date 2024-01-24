using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineOverviewMarkTagger : CoverageLineTaggerBase<OverviewMarkTag>, IListener<CoverageMarginOptionsChangedMessage>
	{
		private ICoverageMarginOptions coverageMarginOptions;
		public CoverageLineOverviewMarkTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, ICoverageMarginOptions coverageMarginOptions) : 
			base(textBuffer, lastCoverageLines)
		{
			this.coverageMarginOptions = coverageMarginOptions;
		}

        public void Handle(CoverageMarginOptionsChangedMessage message)
        {
			coverageMarginOptions = message.Options;
			RaiseTagsChanged();
        }

        protected override TagSpan<OverviewMarkTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
		{
			var coverageType = coverageLine.GetCoverageType();
			var shouldShow = coverageMarginOptions.Show(coverageType);
			if (!shouldShow) return null;

			var newSnapshotSpan = GetLineSnapshotSpan(coverageLine.Number, span);
			return new TagSpan<OverviewMarkTag>(newSnapshotSpan, new OverviewMarkTag(GetMarkKindName(coverageLine)));
		}

		private SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
		{
			var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

			var startPoint = line.Start;
			var endPoint = line.End;

			return new SnapshotSpan(startPoint, endPoint);
		}

		private string GetMarkKindName(Line line)
		{
			var lineHitCount = line?.Hits ?? 0;
			var lineConditionCoverage = line?.ConditionCoverage?.Trim();

			var markKindName = EnterpriseFontsAndColorsNames.CoverageNotTouchedArea;

			if (lineHitCount > 0)
			{
				markKindName = EnterpriseFontsAndColorsNames.CoverageTouchedArea;

				if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
				{
					markKindName = EnterpriseFontsAndColorsNames.CoveragePartiallyTouchedArea;
				}
			}
			return markKindName;
		}
	}
}
