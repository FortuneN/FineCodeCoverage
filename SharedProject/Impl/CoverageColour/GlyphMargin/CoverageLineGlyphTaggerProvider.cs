using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
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
	internal class CoverageLineGlyphTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineGlyphTagger, CoverageLineGlyphTag>
    {
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private RefreshCoverageGlyphsMessage refreshCoverageGlyphsMessage = new RefreshCoverageGlyphsMessage();
        [ImportingConstructor]
		public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator, 
            ICoverageColoursProvider coverageColoursProvider,
            ICoverageColours coverageColours
        ) : base(eventAggregator)
        {
            this.coverageColoursProvider = coverageColoursProvider;
            coverageColours.ColoursChanged += CoverageColours_ColoursChanged;
        }

        private void CoverageColours_ColoursChanged(object sender, System.EventArgs e)
        {
            eventAggregator.SendMessage(refreshCoverageGlyphsMessage);
        }

        protected override void NewCoverageLinesMessageReceived()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await coverageColoursProvider.PrepareAsync();
            });
        }

        protected override CoverageLineGlyphTagger CreateTagger(ITextBuffer textBuffer, SharedProject.Core.Model.FileLineCoverage lastCoverageLines)
        {
            return new CoverageLineGlyphTagger(textBuffer, lastCoverageLines);
        }
    }
}