using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal interface ITestConfigurationFactory
    {
		ITestConfiguration Create(IOperation operation);
	}
}
