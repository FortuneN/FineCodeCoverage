using System;
using FineCodeCoverage.Engine;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine.Model;
using System.Linq;

namespace FineCodeCoverage.Impl
{
	internal class Tagger<T> : ITagger<T> where T : ITag
	{
		private readonly ITextBuffer _textBuffer;
        private readonly IFCCEngine fccEngine;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public Tagger(ITextBuffer textBuffer,IFCCEngine fccEngine)
		{
			_textBuffer = textBuffer;
            this.fccEngine = fccEngine;
            fccEngine.UpdateMarginTags += FCCEngine_UpdateMarginTags;
		}

		private void FCCEngine_UpdateMarginTags(UpdateMarginTagsEventArgs e)
		{
			var span = new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length);
			var spanEventArgs = new SnapshotSpanEventArgs(span);
			TagsChanged?.Invoke(this, spanEventArgs);
		}

		IEnumerable<ITagSpan<T>> ITagger<T>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<T>>();

			if (spans == null || fccEngine.CoverageLines == null)
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
				var coverageLines = GetLines(document.FilePath, startLineNumber, endLineNumber);

				foreach (var coverageLine in coverageLines)
				{
					var tag = new GlyphTag(coverageLine);
					var tagSpan = new TagSpan<GlyphTag>(span, tag);
					var iTagSpan = tagSpan as ITagSpan<T>;
					result.Add(iTagSpan);
				}
			}

			return result;
		}

		private IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber)
		{
			return fccEngine.CoverageLines
			.AsParallel()
			.Where(x => x.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase))
			.Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
			.ToArray();
		}
	}
}