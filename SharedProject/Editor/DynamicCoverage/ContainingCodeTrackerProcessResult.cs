using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTrackerProcessResult : IContainingCodeTrackerProcessResult
    {
        public ContainingCodeTrackerProcessResult(bool changed, List<Span> unprocessedSpans)
        {
            Changed = changed;
            UnprocessedSpans = unprocessedSpans;
        }

        public bool Changed { get; set; }

        public List<Span> UnprocessedSpans { get; }
    }
}
