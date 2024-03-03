using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ICoverageLineFactory))]
    internal class CoverageLineFactory : ICoverageLineFactory
    {
        private readonly ILineTracker lineTracker;

        [ImportingConstructor]
        public CoverageLineFactory(ILineTracker lineTracker) => this.lineTracker = lineTracker;
        public ICoverageLine Create(ITrackingSpan trackingSpan, ILine line) => new CoverageLine(trackingSpan, line, lineTracker);
    }
}
