using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal class CoverletCoberturaCollector : ICoverletCoberturaCollector
    {
		private Dictionary<string, ICoverletCoberturaResultsDirectoryCollector> resultsDirectoryCollectors = new Dictionary<string, ICoverletCoberturaResultsDirectoryCollector>();
		private Dictionary<string, ICoverletCoberturaResultsDirectoryCollector> projectCollectors = new Dictionary<string, ICoverletCoberturaResultsDirectoryCollector>();

		private readonly ICoverletCoberturaResultsDirectoryCollectorFactory resultsDirectoryCollectorFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;
        private readonly IRunSettingsResultsDirectoryForEnabledCollectorParser parser;
        private readonly ITestConfigurationFactory testConfigurationFactory;

        public CoverletCoberturaCollector(
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
		public void CollectFrom(IOperation operation)
        {
			var testConfiguration = testConfigurationFactory.Create(operation);
			var testConfigurationIsValid = testConfiguration.GetIsValid();
            if (testConfigurationIsValid)
            {
				var testContainers = testConfiguration.Containers.Select(c => new TestContainer(c)).Where(tc => tc.GetIsValid());
				foreach (var container in testContainers)
				{
					string testResultsDirectory = null;

					var source = container.Source;

					var runSettingsFile = ThreadHelper.JoinableTaskFactory.Run(() => runSettingsRetriever.GetRunSettingsFileAsync(testConfiguration.UserRunSettings, container.Actual));
					if (!string.IsNullOrEmpty(runSettingsFile))
					{
						testResultsDirectory = parser.Get(runSettingsFile, testConfiguration.ResultsDirectory);
					}

					if (testResultsDirectory != null)
					{
						var hasResultsDirectory = resultsDirectoryCollectors.TryGetValue(testResultsDirectory, out var collector);
						if (!hasResultsDirectory)
						{
							collector = resultsDirectoryCollectorFactory.Create(testResultsDirectory);
							resultsDirectoryCollectors.Add(testResultsDirectory, collector);
						}
						collector.AddProjectCollectingToResultsDirectory(source);
						projectCollectors.Add(source, collector);
					}
				}
			}
		}

		public void Dispose()
        {
            foreach(var collector in resultsDirectoryCollectors.Values)
            {
				collector.Dispose();
            }
        }

		
        public string GetCollected(string testDllFile)
        {
			var hasCollector = projectCollectors.TryGetValue(testDllFile, out var projectCollector);
            if (hasCollector)
            {
				return projectCollector.GetCollected(testDllFile);
			}
			return null;
        }
    }
}
