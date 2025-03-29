using System.Xml.Linq;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal interface IRunSettingsToConfiguration
    {
        XElement ConvertToConfiguration(XElement runSettingsElement);
    }
}
