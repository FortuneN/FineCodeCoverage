using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITrackedLinesFactory))]
    internal class TrackedLinesFactory : ITrackedLinesFactory
    {
        private readonly IRoslynService roslynService;

        [ImportingConstructor]
        public TrackedLinesFactory(
            IRoslynService roslynService
        )
        {
            this.roslynService = roslynService;
        }
        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            if (language == Language.CPP) throw new NotImplementedException();//todo
            var orderedContainingCodeLineRanges = GetOrderedContainingCodeLineRanges(lines, textSnapshot, language);
            return new TrackedLines(lines, textSnapshot, orderedContainingCodeLineRanges);
        }

        private List<ContainingCodeLineRange> GetOrderedContainingCodeLineRanges(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            var roslynContainingCodeLines = ThreadHelper.JoinableTaskFactory.Run(
                () =>
                {
                    var adjustedLineNumbers = lines.Select(l => l.Number - 1).ToList();
                    return roslynService.GetContainingCodeLineRangesAsync(textSnapshot, adjustedLineNumbers);
                }
            );
            // will ensure that is ordered ?
            return roslynContainingCodeLines.OrderBy(containingCodeLine => containingCodeLine.StartLine).ToList();
        }
    }
}
