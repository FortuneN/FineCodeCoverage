using FineCodeCoverage.Core.Initialization;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITestOperationStateInvocationManager))]
    internal class TestOperationStateInvocationManager : ITestOperationStateInvocationManager
    {
        private readonly IInitializeStatusProvider initializeStatusProvider;
        private readonly ILogger logger;
        private bool initializedWhenTestExecutionStarting;

        [ImportingConstructor]
        public TestOperationStateInvocationManager(
            IInitializeStatusProvider initializeStatusProvider,
            ILogger logger
        )
        {
            this.initializeStatusProvider = initializeStatusProvider;
            this.logger = logger;
        }
        public bool CanInvoke(TestOperationStates testOperationState)
        {
            if (testOperationState == TestOperationStates.TestExecutionStarting)
            {
                initializedWhenTestExecutionStarting = initializeStatusProvider.InitializeStatus == InitializeStatus.Initialized;
            }
            if (!initializedWhenTestExecutionStarting)
            {
                logger.Log($"Skipping {testOperationState} as FCC not initialized");
            }
            return initializedWhenTestExecutionStarting;
        }
    }
}
