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
        public ICoverageLine Create(ITrackingSpan trackingSpan, ILine line)
        {
            return new CoverageLine(trackingSpan, line);
        }
    }

}
