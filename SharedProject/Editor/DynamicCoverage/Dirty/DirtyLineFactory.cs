using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IDirtyLineFactory))]
    internal class DirtyLineFactory : IDirtyLineFactory
    {
        private readonly ILineTracker lineTracker;

        [ImportingConstructor]
        public DirtyLineFactory(ILineTracker lineTracker) => this.lineTracker = lineTracker;
        public ITrackingLine Create(ITrackingSpan trackingSpan, ITextSnapshot snapshot) 
            => new TrackingLine(trackingSpan, snapshot, this.lineTracker, DynamicCoverageType.Dirty);
    }
}
