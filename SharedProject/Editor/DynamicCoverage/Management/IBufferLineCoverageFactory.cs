using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IBufferLineCoverageFactory
    {
        IBufferLineCoverage Create(
            IFileLineCoverage fileLineCoverage, ITextInfo textInfo, IEventAggregator eventAggregator, ITrackedLinesFactory trackedLinesFactory
        );
    }
}
