using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn
{
    [Export(typeof(IRoslynFileCodeSpanRangeService))]
    internal class RoslynFileCodeSpanRangeService : IFileCodeSpanRangeService, IRoslynFileCodeSpanRangeService
    {
        private readonly IRoslynService roslynService;
        private readonly IAppOptionsProvider appOptionsProvider;

        [ImportingConstructor]
        public RoslynFileCodeSpanRangeService(IRoslynService roslynService, IAppOptionsProvider appOptionsProvider)
        {

            this.roslynService = roslynService;
            this.appOptionsProvider = appOptionsProvider;
        }

        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            int startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            int endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }

        public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot)
        {
            // will use joinable...
            List<TextSpan> textSpans = this.roslynService.GetContainingCodeSpansAsync(snapshot).GetAwaiter().GetResult();
            return textSpans.Select(textSpan => this.GetCodeSpanRange(textSpan, snapshot)).ToList();
        }

        public IFileCodeSpanRangeService FileCodeSpanRangeService => this;

        public IFileCodeSpanRangeService FileCodeSpanRangeServiceForChanges
            => this.appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges ? null : this;
    }
}
