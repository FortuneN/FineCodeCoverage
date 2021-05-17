using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FineCodeCoverage.Engine;
using FineCodeCoverageTests.Test_helpers;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    class CoverageToolOutput_Exports_Tests
    {
        [Test]
        [Ignore("FileLoadException Microsoft.VisualStudio.Threading")]
        public void ICoverageToolOutputFolderProvider_Should_Have_Consistent_Ordered_Exports()
        {
            MefOrderAssertions.InterfaceExportsHaveConsistentOrder(typeof(ICoverageToolOutputFolderProvider));
        }
    }
}
