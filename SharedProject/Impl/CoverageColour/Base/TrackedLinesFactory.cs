using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
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
        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot)
        {
            var orderedContainingCodeLineRanges = GetOrderedContainingCodeLineRanges(lines, textSnapshot);
            return new TrackedLines(lines, textSnapshot, orderedContainingCodeLineRanges);
        }

        private List<ContainingCodeLineRange> GetOrderedContainingCodeLineRanges(List<ILine> lines, ITextSnapshot textSnapshot)
        {
            var roslynContainingCodeLines = ThreadHelper.JoinableTaskFactory.Run(
                () => roslynService.GetContainingCodeLineRangesAsync(textSnapshot, lines.Select(l => l.Number - 1).ToList())
            );
            // will ensure that is ordered ?
            return roslynContainingCodeLines.OrderBy(containingCodeLine => containingCodeLine.StartLine).ToList();
        }
    }
}
