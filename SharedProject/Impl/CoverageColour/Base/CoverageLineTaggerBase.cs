using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
	interface ILineSpan
	{
		Line Line { get; }
		SnapshotSpan Span { get;}
	}
	internal interface ILineSpanLogic
	{
		IEnumerable<ILineSpan> GetLineSpans(IFileLineCoverage fileLineCoverage,string filePath, NormalizedSnapshotSpanCollection spans);
	}

    internal class LineSpan : ILineSpan
    {
        public LineSpan(Line line, SnapshotSpan span)
        {
            Line = line;
            Span = span;
        }
        public Line Line { get; }

        public SnapshotSpan Span { get; }
    }

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

    internal abstract class CoverageLineTaggerBase<TTag> : 
		IListener<CoverageTypeFilterChangedMessage>,
        IListener<NewCoverageLinesMessage>,
        IDisposable, 
		ITagger<TTag>
		where TTag : ITag

    {
		private readonly ITextBuffer _textBuffer;
		private readonly string filePath;
        private IFileLineCoverage coverageLines;
        private ICoverageTypeFilter coverageTypeFilter;
        private readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public CoverageLineTaggerBase(
			ITextBuffer textBuffer, 
			IFileLineCoverage lastCoverageLines,
            ICoverageTypeFilter coverageTypeFilter,
            IEventAggregator eventAggregator,
			ILineSpanLogic lineSpanLogic
        )
		{
			_textBuffer = textBuffer;
			if(textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
			{
				filePath = document.FilePath;
			}

            coverageLines = lastCoverageLines;
            this.coverageTypeFilter = coverageTypeFilter;
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
        }

		protected void RaiseTagsChanged()
		{
			var span = new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length);
			var spanEventArgs = new SnapshotSpanEventArgs(span);
			TagsChanged?.Invoke(this, spanEventArgs);
		}

		public void Handle(NewCoverageLinesMessage message)
		{
			coverageLines = message.CoverageLines;
			RaiseTagsChanged();
		}

		public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<TTag>>();

			if (filePath == null || coverageLines == null || coverageTypeFilter.Disabled)
			{
				return result;
			}
            var lineSpans = lineSpanLogic.GetLineSpans(coverageLines, filePath, spans);
            return GetTags(lineSpans);
		}

        private IEnumerable<ITagSpan<TTag>> GetTags(IEnumerable<ILineSpan> lineSpans)
		{
            foreach(var lineSpan in lineSpans)
            {
                var line = lineSpan.Line;
                if (!coverageTypeFilter.Show(line.CoverageType))
                {
                    continue;
                }
                var tagSpan = GetTagSpan(line, lineSpan.Span) ;
                yield return tagSpan;
            }
		}

        protected abstract TagSpan<TTag> GetTagSpan(Engine.Cobertura.Line coverageLine, SnapshotSpan span);

        public void Dispose()
        {
            eventAggregator.RemoveListener(this);
        }

        public void Handle(CoverageTypeFilterChangedMessage message)
        {
			if(message.Filter.TypeIdentifier == coverageTypeFilter.TypeIdentifier)
			{
                coverageTypeFilter = message.Filter;
                RaiseTagsChanged();
            }
        }
    }
}
