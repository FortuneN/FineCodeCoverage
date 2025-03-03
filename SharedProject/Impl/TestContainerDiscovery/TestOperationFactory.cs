using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using ReflectObject;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITestOperationFactory))]
    internal class TestOperationFactory : ITestOperationFactory
    {
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;
        private readonly Output.ILogger logger;

        [ImportingConstructor]
        public TestOperationFactory(
            ICoverageProjectFactory coverageProjectFactory,
            IRunSettingsRetriever runSettingsRetriever,
            Output.ILogger logger
            )
        {
            this.coverageProjectFactory = coverageProjectFactory;
            this.runSettingsRetriever = runSettingsRetriever;
            this.logger = logger;
        }
        public ITestOperation Create(IOperation operation)
        {
            try
            {
                return new TestOperation(new TestRunRequest(operation), coverageProjectFactory, runSettingsRetriever);
            }
            catch (PropertyDoesNotExistException propertyDoesNotExistException)
            {
                logger.Log("Error test container discoverer reflection");
                throw new Exception(propertyDoesNotExistException.Message);
            }
        }
    }
}



