using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal interface ITestOperationStateInvocationManager
    {
        bool CanInvoke(TestOperationStates testOperationState);
    }
}
