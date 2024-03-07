using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    [Export(typeof(ILineSpanLogic))]
    internal class LineSpanLogic : ILineSpanLogic
    {
        public IEnumerable<ILineSpan> GetLineSpans(
            IBufferLineCoverage bufferLineCoverage,
            NormalizedSnapshotSpanCollection normalizedSnapshotSpanCollection
        ) => normalizedSnapshotSpanCollection.SelectMany(snapshotSpan => GetApplicableLineSpans(snapshotSpan, bufferLineCoverage));

        private static IEnumerable<ILineSpan> GetApplicableLineSpans(SnapshotSpan snapshotSpan, IBufferLineCoverage bufferLineCoverage)
        {
            IEnumerable<IDynamicLine> applicableCoverageLines = GetApplicableCoverageLines(bufferLineCoverage, snapshotSpan);
            return applicableCoverageLines.Select(
                applicableCoverageLine => new LineSpan(applicableCoverageLine, GetLineSnapshotSpan(applicableCoverageLine.Number, snapshotSpan)));
        }

        private static IEnumerable<IDynamicLine> GetApplicableCoverageLines(IBufferLineCoverage bufferLineCoverage, SnapshotSpan span)
        {
            (int coverageStartLineNumber, int coverageEndLineNumber) = GetStartEndCoverageLineNumbers(span);
            return bufferLineCoverage.GetLines(coverageStartLineNumber, coverageEndLineNumber);
        }

        private static (int, int) GetStartEndCoverageLineNumbers(SnapshotSpan span)
        {
            int startLineNumber = span.Start.GetContainingLine().LineNumber;
            int endLineNumber = span.End.GetContainingLine().LineNumber;
            return (startLineNumber, endLineNumber);
        }

        private static SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
        {
            ITextSnapshotLine line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber);

            SnapshotPoint startPoint = line.Start;
            SnapshotPoint endPoint = line.End;

            return new SnapshotSpan(startPoint, endPoint);
        }
    }
}
