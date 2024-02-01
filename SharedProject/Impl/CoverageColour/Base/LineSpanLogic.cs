using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ILineSpanLogic))]
    internal class LineSpanLogic : ILineSpanLogic
    {
        public IEnumerable<ILineSpan> GetLineSpans(IFileLineCoverage fileLineCoverage, string filePath, NormalizedSnapshotSpanCollection spans)
        {
			var lineSpans = new List<ILineSpan>();
            foreach (var span in spans)
            {
                var startLineNumber = span.Start.GetContainingLine().LineNumber + 1;
                var endLineNumber = span.End.GetContainingLine().LineNumber + 1;

                var applicableCoverageLines = fileLineCoverage.GetLines(filePath, startLineNumber, endLineNumber);


                foreach (var applicableCoverageLine in applicableCoverageLines)
                {
                    var lineSnapshotSpan = GetLineSnapshotSpan(applicableCoverageLine.Number, span);
                    lineSpans.Add(new LineSpan(applicableCoverageLine, lineSnapshotSpan));
                }
            }
            return lineSpans;
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
