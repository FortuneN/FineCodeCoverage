using System;
using FineCodeCoverage.Engine;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;
using System.Linq;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTagger : ITagger<CoverageLineGlyphTag>, IListener<NewCoverageLinesMessage>
	{
		private readonly ITextBuffer _textBuffer;
		private List<CoverageLine> coverageLines;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public CoverageLineGlyphTagger(ITextBuffer textBuffer, List<CoverageLine> lastCoverageLines)
		{
			_textBuffer = textBuffer;
			coverageLines = lastCoverageLines;
			if (lastCoverageLines != null)
            {
				RaiseTagsChanged();
            }
		}

		private void RaiseTagsChanged()
		{
			var span = new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length);
			var spanEventArgs = new SnapshotSpanEventArgs(span);
			TagsChanged?.Invoke(this, spanEventArgs);
		}

		IEnumerable<ITagSpan<CoverageLineGlyphTag>> ITagger<CoverageLineGlyphTag>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<CoverageLineGlyphTag>>();

			if (spans == null || coverageLines == null)
			{
				return result;
			}

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
					var tag = new CoverageLineGlyphTag(applicableCoverageLine);
					var tagSpan = new TagSpan<CoverageLineGlyphTag>(span, tag);
					result.Add(tagSpan);
				}
			}

			return result;
		}

		private IEnumerable<CoverageLine> GetApplicableLines(string filePath, int startLineNumber, int endLineNumber)
		{
			return coverageLines
			.AsParallel()
			.Where(x => x.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase))
			.Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
			.ToArray();
		}

        public void Handle(NewCoverageLinesMessage message)
        {
            coverageLines = message.CoverageLines;
			RaiseTagsChanged();
        }
    }
}