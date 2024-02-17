using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IDirtyLineFactory))]
    internal class DirtyLineFactory : IDirtyLineFactory
    {
        public IDirtyLine Create(ITrackingSpan trackingSpan, ITextSnapshot snapshot)
        {
            return new DirtyLine(trackingSpan, snapshot);
        }
    }
}
