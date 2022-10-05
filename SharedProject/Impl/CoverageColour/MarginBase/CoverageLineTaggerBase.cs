using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Impl
{
	internal abstract class CoverageLineTaggerBase<TTag> : ICoverageLineTagger<TTag> where TTag : ITag
	{
		private readonly ITextBuffer _textBuffer;
		private Dictionary<string, List<CoverageLine>> coverageLines;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public CoverageLineTaggerBase(ITextBuffer textBuffer, Dictionary<string, List<CoverageLine>> lastCoverageLines)
		{
			_textBuffer = textBuffer;
			coverageLines = lastCoverageLines;
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

		private IEnumerable<CoverageLine> GetApplicableLines(string filePath, int startLineNumber, int endLineNumber)
		{
			return coverageLines[filePath]
			.AsParallel()
			.Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
			.ToArray();
		}

		public void Handle(NewCoverageLinesMessage message)
		{
			coverageLines = message.CoverageLines;
			RaiseTagsChanged();
		}

		public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<TTag>>();

			if (spans == null || coverageLines == null)
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
					var tagSpan = GetTagSpan(applicableCoverageLine, span);
					if (tagSpan != null)
                    {
						result.Add(GetTagSpan(applicableCoverageLine, span));
					}
				}
			}
		}

		protected abstract TagSpan<TTag> GetTagSpan(CoverageLine coverageLine, SnapshotSpan span);
	}
}
