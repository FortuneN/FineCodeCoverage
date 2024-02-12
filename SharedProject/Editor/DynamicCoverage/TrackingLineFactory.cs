﻿using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackingLineFactory))]
    public class TrackingLineFactory : ITrackingLineFactory
    {
        public ITrackingSpan Create(ITextSnapshot textSnapshot, int lineNumber)
        {
            var span = textSnapshot.GetLineFromLineNumber(lineNumber).Extent;
            return textSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
        }
    }
}