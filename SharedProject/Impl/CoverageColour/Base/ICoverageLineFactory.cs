using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface ICoverageLineFactory
    {
        ICoverageLine Create(ITrackingSpan trackingSpan, ILine line);
    }

}
