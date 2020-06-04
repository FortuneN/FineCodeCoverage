using System.Windows.Media;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
	[TagType(typeof(GlyphTag))]
	[Export(typeof(ITaggerProvider))]
	[Name(ProjectMetaData.TaggerProviderName)]
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
			foreach (var span in spans)
			{
				var document = span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

				if (document == null)
				{
					continue;
				}

				var lineNumber = span.Start.GetContainingLine().LineNumber + 1;

				var coverageLine = CoverageUtil.GetCoverageLine(document.FilePath, lineNumber);

				if (coverageLine == null)
				{
					continue;
				}

				var lineGlyphTag = new GlyphTag();
				lineGlyphTag.Rectangle1.Width = 2;
				lineGlyphTag.Rectangle1.Height = 16;
				lineGlyphTag.Rectangle1.Fill = coverageLine.HitCount > 0 ? Brushes.Green : Brushes.Red;
                //ToolTipService.SetToolTip(lineGlyphTag, new ToolTip { Content = $"{coverageLine.HitCount} Hits" });

				yield return new TagSpan<GlyphTag>(span, lineGlyphTag);
			}
		}
	}
}