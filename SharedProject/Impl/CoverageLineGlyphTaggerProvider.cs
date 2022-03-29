using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Shell;
using FineCodeCoverage.Core.Utilities;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(CoverageLineGlyphTag))]
	[Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : ITaggerProvider, IListener<NewCoverageLinesMessage>
	{
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private List<CoverageLine> lastCoverageLines;

        [ImportingConstructor]
		public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator, 
            ICoverageColoursProvider coverageColoursProvider)
        {
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
		{
			var coverageLineGlyphTagger =  new CoverageLineGlyphTagger(textBuffer, lastCoverageLines);
            eventAggregator.AddListener(coverageLineGlyphTagger, false);
            return coverageLineGlyphTagger as ITagger<T>;
		}

        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            ThreadHelper.JoinableTaskFactory.Run(async () =>
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
            {
                await coverageColoursProvider.PrepareAsync();
            });
        }
    }
}