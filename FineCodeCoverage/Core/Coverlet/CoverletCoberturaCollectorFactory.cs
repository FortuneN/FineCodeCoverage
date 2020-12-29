using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverletCoberturaCollectorFactory))]
	internal class CoverletCoberturaCollectorFactory : ICoverletCoberturaCollectorFactory
    {
        private readonly ICoverletCoberturaResultsDirectoryCollectorFactory resultsDirectoryCollectorFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;
        private readonly IRunSettingsResultsDirectoryForEnabledCollectorParser parser;
        private readonly ITestConfigurationFactory testConfigurationFactory;

        [ImportingConstructor]
        public CoverletCoberturaCollectorFactory(
			ICoverletCoberturaResultsDirectoryCollectorFactory resultsDirectoryCollectorFactory,
			IRunSettingsRetriever runSettingsRetriever, 
			IRunSettingsResultsDirectoryForEnabledCollectorParser parser,
			ITestConfigurationFactory testConfigurationFactory
			)
        {
            this.resultsDirectoryCollectorFactory = resultsDirectoryCollectorFactory;
            this.runSettingsRetriever = runSettingsRetriever;
            this.parser = parser;
            this.testConfigurationFactory = testConfigurationFactory;
        }
		public ICoverletCoberturaCollector Create(IOperation operation)
        {
			var collector = new CoverletCoberturaCollector(this.resultsDirectoryCollectorFactory, runSettingsRetriever,parser,testConfigurationFactory);
			collector.CollectFrom(operation);
			return collector;

		}
    }
}
