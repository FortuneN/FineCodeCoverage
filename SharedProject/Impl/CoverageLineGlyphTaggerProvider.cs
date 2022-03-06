using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(CoverageLineGlyphTag))]
	[Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : ITaggerProvider
	{
        private readonly IFCCEngine fccEngine;
        private readonly ICoverageColoursProvider coverageColoursProvider;

        [ImportingConstructor]
		public CoverageLineGlyphTaggerProvider(
            IFCCEngine fccEngine, 
            ICoverageColoursProvider coverageColoursProvider)
        {
            this.fccEngine = fccEngine;
            fccEngine.UpdateMarginTags += FccEngine_UpdateMarginTags;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        private void FccEngine_UpdateMarginTags(UpdateMarginTagsEventArgs e)
        {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
			ThreadHelper.JoinableTaskFactory.Run(async () =>
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
			{
				await coverageColoursProvider.PrepareAsync();
			});
			
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
		{
			return new CoverageLineGlyphTagger(textBuffer, fccEngine) as ITagger<T>;
		}
	}
}