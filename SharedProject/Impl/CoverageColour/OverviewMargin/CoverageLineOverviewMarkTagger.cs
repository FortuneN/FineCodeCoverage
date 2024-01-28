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
        private readonly ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper;

        public CoverageLineOverviewMarkTagger(
			ITextBuffer textBuffer, 
			FileLineCoverage lastCoverageLines, 
			ICoverageMarginOptions coverageMarginOptions, 
			IEventAggregator eventAggregator,
            ICoverageLineCoverageTypeInfoHelper coverageLineCoverageTypeInfoHelper
        ) : base(textBuffer, lastCoverageLines,eventAggregator)
		{
			this.coverageMarginOptions = coverageMarginOptions;
            this.coverageLineCoverageTypeInfoHelper = coverageLineCoverageTypeInfoHelper;
        }

        public void Handle(CoverageMarginOptionsChangedMessage message)
        {
			coverageMarginOptions = message.Options;
			RaiseTagsChanged();
        }

        protected override TagSpan<OverviewMarkTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span)
		{
			var coverageTypeInfo = coverageLineCoverageTypeInfoHelper.GetInfo(coverageLine);

			var shouldShow = coverageMarginOptions.Show(coverageTypeInfo.CoverageType);
			if (!shouldShow) return null;

			var newSnapshotSpan = GetLineSnapshotSpan(coverageLine.Number, span);
			return new TagSpan<OverviewMarkTag>(newSnapshotSpan, new OverviewMarkTag(coverageTypeInfo.EditorFormatDefinitionName));
		}

		private SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
		{
			var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

			var startPoint = line.Start;
			var endPoint = line.End;

			return new SnapshotSpan(startPoint, endPoint);
		}

	}
}
