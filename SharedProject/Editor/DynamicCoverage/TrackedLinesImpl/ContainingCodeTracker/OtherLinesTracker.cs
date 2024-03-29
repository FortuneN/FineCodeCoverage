﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class OtherLinesTracker : IUpdatableDynamicLines
    {
        public IEnumerable<IDynamicLine> Lines { get; } = Enumerable.Empty<IDynamicLine>();

        public ContainingCodeTrackerType Type => ContainingCodeTrackerType.OtherLines;

        public IEnumerable<int> GetUpdatedLineNumbers(
            TrackingSpanRangeProcessResult trackingSpanRangeProcessResult,
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanAndLineRanges) => Enumerable.Empty<int>();
    }
}
