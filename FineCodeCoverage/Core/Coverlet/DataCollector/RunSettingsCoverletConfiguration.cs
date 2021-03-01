using System.Linq;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Coverlet
{
    internal class RunSettingsCoverletConfiguration : IRunSettingsCoverletConfiguration
    {
        public bool Read(string runSettingsXml)
        {
            var document = XDocument.Parse(runSettingsXml);
            //<DataCollector friendlyName=""XPlat code coverage"">
            var coverletDataCollectorElement = document.Descendants("DataCollector").FirstOrDefault(dataCollector =>
            {
                var friendlyNameAttribute = dataCollector.Attribute("friendlyName");
                return (friendlyNameAttribute == null ? "" : friendlyNameAttribute.Value) == "XPlat code coverage";
            });

            if(coverletDataCollectorElement != null)
            {
                var enabledAttribute = coverletDataCollectorElement.Attribute("enabled");
                if(enabledAttribute == null)
                {
                    CoverletDataCollectorState = CoverletDataCollectorState.Enabled;
                }
                else
                {
                    CoverletDataCollectorState = enabledAttribute.Value.ToLower() == "true" ? CoverletDataCollectorState.Enabled : CoverletDataCollectorState.Disabled;
                }
            }
            else
            {
                return false;
            }

            var configurationElement = coverletDataCollectorElement.Element("Configuration");
            if(configurationElement == null)
            {
                return false;
            }
            var configurationElements = configurationElement.Elements().ToList();
            if(configurationElements.Count == 0)
            {
                return false;
            }

            bool foundElements = false;
            this.GetType().GetProperties().ToList().ForEach(p =>
            {
                var configurationPropertyElement = configurationElements.FirstOrDefault(e => e.Name == p.Name);
                if(configurationPropertyElement != null)
                {
                    foundElements = true;
                    p.SetValue(this, configurationPropertyElement.Value);
                }
            });

            return foundElements;
        }

        public CoverletDataCollectorState CoverletDataCollectorState { get; private set; }

        public string Format { get; private set; }
        public string Exclude { get; private set; }
        public string Include { get; private set; }
        public string ExcludeByAttribute { get; private set; }
        public string ExcludeByFile { get; private set; }
        public string IncludeDirectory { get; private set; }
        public string SingleHit { get; private set; }
        public string UseSourceLink { get; private set; }
        public string IncludeTestAssembly { get; private set; }
        public string SkipAutoProps { get; private set; }
    }
}
