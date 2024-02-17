using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IBufferLineCoverageFactory))]
    internal class BufferLineCoverageFactory : IBufferLineCoverageFactory
    {
        public IBufferLineCoverage Create(
            IFileLineCoverage fileLineCoverage, 
            TextInfo textInfo, 
            IEventAggregator eventAggregator, 
            ITrackedLinesFactory trackedLinesFactory)
        {
            return new BufferLineCoverage(fileLineCoverage, textInfo, eventAggregator, trackedLinesFactory);
        }
    }
}
