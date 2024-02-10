using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(IShouldAddCoverageMarkersLogic))]
    class ShouldAddCoverageMarkersLogic : IShouldAddCoverageMarkersLogic
    {
        public bool ShouldAddCoverageMarkers()
        {
            return !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.CodeCoverage.VisualStudio.Window");
        }
    }

}
