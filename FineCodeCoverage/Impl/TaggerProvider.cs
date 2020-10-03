using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using System.Linq;
using System;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(GlyphTag))]
	[Export(typeof(ITaggerProvider))]
	[Name(Vsix.TaggerProviderName)]
	internal class TaggerProvider : ITaggerProvider
	{
		private static readonly List<Tagger<GlyphTag>> CachedTaggers = new List<Tagger<GlyphTag>>();

		public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
		{
			if (textBuffer == null)
			{
				return null;
			}

			var tagger = new Tagger<GlyphTag>(textBuffer, GetTags);
			CachedTaggers.Add(tagger);
			return tagger as ITagger<T>;
		}

		public static void ReloadTags()
		{
			foreach (var tagger in CachedTaggers)
			{
				tagger.FireTagsChangedEvent();
			}
		}

		private IEnumerable<ITagSpan<GlyphTag>> GetTags(ITextBuffer textBuffer, NormalizedSnapshotSpanCollection spans)
		{
			var document = (ITextDocument)textBuffer.Properties.GetProperty(typeof(ITextDocument));

			if (document != null)
			{
				foreach (var span in spans)
				{
					var lineNumber = span.Start.GetContainingLine().LineNumber + 1;

					var coverageLine = CoverageUtil.GetLine(document.FilePath, lineNumber);
						
					if (coverageLine == null)
					{
						continue;
					}

					yield return new TagSpan<GlyphTag>(span, new GlyphTag(coverageLine));
				}
			}
		}
	}
}