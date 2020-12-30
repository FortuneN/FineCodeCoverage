using System.Linq;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{

    [Export(typeof(IRunSettingsResultsDirectoryForEnabledCollectorParser))]
    internal class RunSettingsResultsDirectoryForEnabledCollectorParser : IRunSettingsResultsDirectoryForEnabledCollectorParser
    {
        private readonly IFileSystem fileSystem;
        private readonly IXDocumentLoader xDocumentLoader;

        [ImportingConstructor]
		public RunSettingsResultsDirectoryForEnabledCollectorParser(IFileSystem fileSystem, IXDocumentLoader xDocumentLoader)
        {
            this.fileSystem = fileSystem;
            this.xDocumentLoader = xDocumentLoader;
        }
		public string Get(string runSettingsPath,string defaultTestResultsDirectory)
        {
			string testResultsDirectory = null;
			var hasDataCollector = false;
            if (fileSystem.Exists(runSettingsPath))
            {
				var runSettings = xDocumentLoader.Load(runSettingsPath);

				var dataCollectionRunSettings = runSettings.Root.Element("DataCollectionRunSettings");
				if (dataCollectionRunSettings != null)
				{
					var dataCollectorsElement = dataCollectionRunSettings.Element("DataCollectors");
					if (dataCollectorsElement != null)
					{
						var dataCollectorElements = dataCollectorsElement.Elements("DataCollector");
						var coverletDataCollectorElement = dataCollectorElements.SingleOrDefault(el => el.Attribute("friendlyName")?.Value == "XPlat code coverage");
						if (coverletDataCollectorElement != null)
						{
							var enabledAttribute = coverletDataCollectorElement.Attribute("enabled");
							if (enabledAttribute == null || enabledAttribute.Value == "true")
							{
								hasDataCollector = true;
							}
						}
					}

				}
				if (hasDataCollector)
				{
					testResultsDirectory = defaultTestResultsDirectory as string;
					var runConfiguration = runSettings.Root.Element("RunConfiguration");
					if (runConfiguration != null)
					{
						var resultsDirectoryElement = runConfiguration.Element("ResultsDirectory");
						if (resultsDirectoryElement != null)
						{
							var resultsDirectoryMayBeRelative = resultsDirectoryElement.Value;
							testResultsDirectory = fileSystem.EnsureAbsolute(resultsDirectoryMayBeRelative,fileSystem.GetDirectoryName(runSettingsPath));
						}
					}
				}
			}

			return testResultsDirectory;
		}
	}
}
