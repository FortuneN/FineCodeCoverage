using System.ComponentModel.Composition;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITestOperationFactory))]
    internal class TestOperationFactory : ITestOperationFactory
    {
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;
        [ImportingConstructor]
        public TestOperationFactory(ICoverageProjectFactory coverageProjectFactory,
                IRunSettingsRetriever runSettingsRetriever)
        {
            this.coverageProjectFactory = coverageProjectFactory;
            this.runSettingsRetriever = runSettingsRetriever;
        }
        public ITestOperation Create(IOperation operation)
        {
            return new TestOperation(new Operation(operation), coverageProjectFactory, runSettingsRetriever);
        }
    }
}



