using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal interface ITestOperationFactory
    {
        ITestOperation Create(IOperation operation);
    }
}


