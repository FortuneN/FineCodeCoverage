using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	internal class Tagger<T> : ITagger<T> where T: ITag
	{
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		private readonly ITextBuffer _textBuffer;
		private readonly Func<ITextBuffer, NormalizedSnapshotSpanCollection, IEnumerable<ITagSpan<T>>> _getTagsFunc;

		internal void FireTagsChangedEvent()
		{
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length)));
		}

		public Tagger(ITextBuffer textBuffer, Func<ITextBuffer, NormalizedSnapshotSpanCollection, IEnumerable<ITagSpan<T>>> getTagsFunc)
		{
			_textBuffer = textBuffer;
			_getTagsFunc = getTagsFunc;
		}

		IEnumerable<ITagSpan<T>> ITagger<T>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			return _getTagsFunc(_textBuffer, spans);
		}
	}
}