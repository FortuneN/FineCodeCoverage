using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ICoverageLineFactory
    {
        ICoverageLine Create(ITrackingSpan trackingSpan, ILine line);
    }

}
