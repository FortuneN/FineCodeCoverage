﻿using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IUpdatableDynamicLines
    {
        IEnumerable<IDynamicLine> Lines { get; }
        ContainingCodeTrackerType Type { get; }

        IEnumerable<int> GetUpdatedLineNumbers(
            TrackingSpanRangeProcessResult trackingSpanRangeProcessResult,
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanAndLineRanges);
    }
}
