using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(GlyphTag))]
	[Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class TaggerProvider : ITaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
		{
			return new Tagger<T>(textBuffer);
		}
	}
}