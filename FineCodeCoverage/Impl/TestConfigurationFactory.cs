using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITestConfigurationFactory))]
    internal class TestConfigurationFactory : ITestConfigurationFactory
    {
        public ITestConfiguration Create(IOperation operation)
        {
			return new TestConfiguration(operation); 
        }
    }
}
