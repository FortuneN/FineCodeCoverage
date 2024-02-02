using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ILineSpanLogic))]
    internal class LineSpanLogic : ILineSpanLogic
    {
        public IEnumerable<ILineSpan> GetLineSpans(
            IFileLineCoverage fileLineCoverage, 
            string filePath, 
            NormalizedSnapshotSpanCollection normalizedSnapshotSpanCollection)
        {
            return normalizedSnapshotSpanCollection.SelectMany(snapshotSpan => GetApplicableLineSpans(snapshotSpan, fileLineCoverage, filePath));
        }

        private static IEnumerable<ILineSpan> GetApplicableLineSpans(SnapshotSpan snapshotSpan, IFileLineCoverage fileLineCoverage, string filePath)
        {
            var applicableCoverageLines = GetApplicableCoverageLines(fileLineCoverage, filePath, snapshotSpan);
            return applicableCoverageLines.Select(applicableCoverageLine => new LineSpan(applicableCoverageLine, GetLineSnapshotSpan(applicableCoverageLine.Number, snapshotSpan)));
        }

        private static IEnumerable<ILine> GetApplicableCoverageLines(IFileLineCoverage fileLineCoverage,string filePath,SnapshotSpan span)
        {
            var (coverageStartLineNumber, coverageEndLineNumber) = GetStartEndCoverageLineNumbers(span);
            return fileLineCoverage.GetLines(filePath, coverageStartLineNumber, coverageEndLineNumber);
        }

        private static (int, int) GetStartEndCoverageLineNumbers(SnapshotSpan span)
        {
            var startLineNumber = span.Start.GetContainingLine().LineNumber + 1;
            var endLineNumber = span.End.GetContainingLine().LineNumber + 1;
            return (startLineNumber, endLineNumber);
        }

        private static SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
        {
            var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

            var startPoint = line.Start;
            var endPoint = line.End;

            return new SnapshotSpan(startPoint, endPoint);
        }
    }

}
