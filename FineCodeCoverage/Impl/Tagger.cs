using System;
using FineCodeCoverage.Engine;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	internal class Tagger<T> : ITagger<T> where T : ITag
	{
		private readonly ITextBuffer _textBuffer;
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public Tagger(ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
			TestContainerDiscoverer.UpdateMarginTags += TestContainerDiscoverer_UpdateMarginTags;
		}

		private void TestContainerDiscoverer_UpdateMarginTags(object sender, UpdateMarginTagsEventArgs e)
		{
			var span = new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length);
			var spanEventArgs = new SnapshotSpanEventArgs(span);
			TagsChanged?.Invoke(this, spanEventArgs);
		}

		IEnumerable<ITagSpan<T>> ITagger<T>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var result = new List<ITagSpan<T>>();

			if (spans == null)
			{
				return result;
			}

			foreach (var span in spans)
			{
				var startLineNumber = span.Start.GetContainingLine().LineNumber + 1;
				var endLineNumber = startLineNumber;
				
				try { endLineNumber = span.End.GetContainingLine().LineNumber + 1; } catch { }
				
				var document = (ITextDocument)span.Snapshot.TextBuffer.Properties.GetProperty(typeof(ITextDocument));
				var coverageLines = FCCEngine.GetLines(document.FilePath, startLineNumber, endLineNumber);

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
	}
}