using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using System.Xml.Linq;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IRunSettingsResultsDirectoryForEnabledCollectorParser))]
    internal class RunSettingsResultsDirectoryForEnabledCollectorParser : IRunSettingsResultsDirectoryForEnabledCollectorParser
    {
        public string Get(string runSettingsPath,string defaultTestResultsDirectory)
        {
			string testResultsDirectory = null;
			var hasDataCollector = false;
            if (File.Exists(runSettingsPath))
            {
				var runSettings = XDocument.Load(runSettingsPath);

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
							var relative = true;//todo
							if (relative)
							{
								testResultsDirectory = Path.Combine(Path.GetDirectoryName(runSettingsPath), resultsDirectoryMayBeRelative);
							}
							else
							{
								testResultsDirectory = resultsDirectoryMayBeRelative;
							}

						}
					}
				}
			}

			return testResultsDirectory;
		}
	}
}
