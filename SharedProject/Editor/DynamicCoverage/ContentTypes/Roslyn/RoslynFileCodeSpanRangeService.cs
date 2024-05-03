using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn
{
    [Export(typeof(IRoslynFileCodeSpanRangeService))]
    internal class RoslynFileCodeSpanRangeService : IFileCodeSpanRangeService, IRoslynFileCodeSpanRangeService
    {
        private readonly IRoslynService roslynService;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public RoslynFileCodeSpanRangeService(
            IRoslynService roslynService, 
            IAppOptionsProvider appOptionsProvider,
            IThreadHelper threadHelper
            )
        {

            this.roslynService = roslynService;
            this.appOptionsProvider = appOptionsProvider;
            this.threadHelper = threadHelper;
        }

        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            int startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            int endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }

        public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot)
        {
            List<TextSpan> textSpans = this.threadHelper.JoinableTaskFactory.Run(
                () => this.roslynService.GetContainingCodeSpansAsync(snapshot)
            );

            return textSpans.Select(textSpan => this.GetCodeSpanRange(textSpan, snapshot)).ToList();
        }

        public IFileCodeSpanRangeService FileCodeSpanRangeService => this;

        public bool UseFileCodeSpanRangeServiceForChanges
            => this.appOptionsProvider.Get().EditorCoverageColouringMode != EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges;
    }
}
