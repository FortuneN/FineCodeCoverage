using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	internal class Tagger<T> : ITagger<T> where T : ITag
	{
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public Tagger(ITextBuffer textBuffer)
		{
			TestContainerDiscoverer.UpdateMarginTags += (sender, args) => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length)));
		}

		IEnumerable<ITagSpan<T>> ITagger<T>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<T>>();

			if (spans == null || !spans.Any())
			{
				return result;
			}

			foreach (var span in spans)
			{
				var startLineNumber = span.Start.GetContainingLine().LineNumber + 1;
				var endLineNumber = span.End.GetContainingLine().LineNumber + 1;
				var document = (ITextDocument)span.Snapshot.TextBuffer.Properties.GetProperty(typeof(ITextDocument));
				var coverageLines = FCCEngine.GetLines(document.FilePath, startLineNumber, endLineNumber);

				foreach (var coverageLine in coverageLines)
				{
					result.Add(new TagSpan<GlyphTag>(span, new GlyphTag(coverageLine)) as ITagSpan<T>);
				}
			}

			return result;
		}
	}
}