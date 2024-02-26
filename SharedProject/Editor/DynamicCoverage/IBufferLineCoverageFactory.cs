using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IBufferLineCoverageFactory
    {
        IBufferLineCoverage Create(
            IFileLineCoverage fileLineCoverage, TextInfo textInfo, IEventAggregator eventAggregator, ITrackedLinesFactory trackedLinesFactory
        );
    }
}
