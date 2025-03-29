using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(IRunSettingsToConfiguration))]
    internal class RunSettingsToConfiguration : IRunSettingsToConfiguration
    {
        public XElement ConvertToConfiguration(XElement runSettingsElement)
        {
            var dataCollectorsElement = runSettingsElement.Element("DataCollectionRunSettings").Element("DataCollectors");
            var codeCoverageDataCollectorElement = dataCollectorsElement.Elements().FirstOrDefault(dataCollectorElement =>
            {
                var friendlyName = dataCollectorElement.Attribute((XName)"friendlyName")?.Value ?? string.Empty;
                return friendlyName.Equals("Code Coverage", StringComparison.OrdinalIgnoreCase);
            });
            return codeCoverageDataCollectorElement?.Element("Configuration");
        }
    }
}
