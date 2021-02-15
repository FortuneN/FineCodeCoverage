using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Coverlet
{
    internal interface IRunSettingsCoverletConfiguration
    {
        bool Extract(string runSettingsXml); 
        string Format { get; }
        string Exclude { get; }
        string Include { get; }
        string ExcludeByAttribute { get; }
        string ExcludeByFile { get; }
        string IncludeDirectory { get; }
        string SingleHit { get; }
        string UseSourceLink { get; }
        string IncludeTestAssembly { get; }
        string SkipAutoProps { get; }
    }

    [Export(typeof(IRunSettingsCoverletConfiguration))]
    internal class RunSettingsCoverletConfiguration : IRunSettingsCoverletConfiguration
    {
        public bool Extract(string runSettingsXml)
        {
            var document = XDocument.Parse(runSettingsXml);
            //<DataCollector friendlyName=""XPlat code coverage"">
            var coverletDataCollectorElement = document.Descendants("DataCollector").FirstOrDefault(dataCollector =>
            {
                var friendlyNameAttribute = dataCollector.Attribute("friendlyName");
                return (friendlyNameAttribute == null ? "" : friendlyNameAttribute.Value) == "XPlat code coverage";
            });

            if(coverletDataCollectorElement == null)
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
