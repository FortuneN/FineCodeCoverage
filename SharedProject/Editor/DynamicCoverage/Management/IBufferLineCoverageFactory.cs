using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IBufferLineCoverageFactory
    {
        IBufferLineCoverage Create(
            LastCoverage lastCoverage, ITextInfo textInfo, IEventAggregator eventAggregator, ITrackedLinesFactory trackedLinesFactory
        );
    }
}
