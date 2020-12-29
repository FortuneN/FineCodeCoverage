using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverletCoberturaCollectorFactory
    {
		ICoverletCoberturaCollector Create(IOperation operation);
	}
}
