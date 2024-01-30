using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
	internal abstract class CoverageLineTaggerBase<TTag> : 
		IListener<CoverageTypeFilterChangedMessage>,
        IListener<NewCoverageLinesMessage>,
        IDisposable, 
		ITagger<TTag>
		where TTag : ITag

    {
		private readonly ITextBuffer _textBuffer;
		private FileLineCoverage coverageLines;
        private ICoverageTypeFilter coverageTypeFilter;
        private readonly IEventAggregator eventAggregator;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public CoverageLineTaggerBase(
			ITextBuffer textBuffer, 
			FileLineCoverage lastCoverageLines,
            ICoverageTypeFilter coverageTypeFilter,
            Core.Utilities.IEventAggregator eventAggregator
        )
		{
			_textBuffer = textBuffer;
			coverageLines = lastCoverageLines;
            this.coverageTypeFilter = coverageTypeFilter;
            this.eventAggregator = eventAggregator;
            if (lastCoverageLines != null)
			{
				RaiseTagsChanged();
			}
		}

		protected virtual void RaiseTagsChanged()
		{
			var span = new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length);
			var spanEventArgs = new SnapshotSpanEventArgs(span);
			TagsChanged?.Invoke(this, spanEventArgs);
		}

		private IEnumerable<Engine.Cobertura.Line> GetApplicableLines(string filePath, int startLineNumber, int endLineNumber) 
			=> coverageLines.GetLines(filePath, startLineNumber, endLineNumber);

		public void Handle(NewCoverageLinesMessage message)
		{
			coverageLines = message.CoverageLines;
			RaiseTagsChanged();
		}

		public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<TTag>>();

			if (spans == null || coverageLines == null || coverageTypeFilter.Disabled)
			{
				return result;
			}

			AddTags(spans, result);
			return result;
		}

		protected virtual void AddTags(NormalizedSnapshotSpanCollection spans, List<ITagSpan<TTag>> result)
		{
			foreach (var span in spans)
			{
				if (!span.Snapshot.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
				{
					continue;
				}

				var startLineNumber = span.Start.GetContainingLine().LineNumber + 1;
				var endLineNumber = span.End.GetContainingLine().LineNumber + 1;

				var applicableCoverageLines = GetApplicableLines(document.FilePath, startLineNumber, endLineNumber);

				foreach (var applicableCoverageLine in applicableCoverageLines)
				{
					if (!coverageTypeFilter.Show(applicableCoverageLine.CoverageType))
					{
                        continue;
                    }
					var tagSpan = GetTagSpan(applicableCoverageLine, span);
                    result.Add(tagSpan);
				}
			}
		}

        protected SnapshotSpan GetLineSnapshotSpan(int lineNumber, SnapshotSpan originalSpan)
        {
            var line = originalSpan.Snapshot.GetLineFromLineNumber(lineNumber - 1);

            var startPoint = line.Start;
            var endPoint = line.End;

            return new SnapshotSpan(startPoint, endPoint);
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
