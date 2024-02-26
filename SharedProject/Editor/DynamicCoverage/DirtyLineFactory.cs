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
        public DirtyLineFactory(ILineTracker lineTracker) {
            this.lineTracker = lineTracker;
        }
        public IDirtyLine Create(ITrackingSpan trackingSpan, ITextSnapshot snapshot)
        {
            return new DirtyLine(trackingSpan, snapshot, lineTracker);
        }
    }
}
