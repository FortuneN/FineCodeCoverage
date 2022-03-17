using System.Linq;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    internal static class RunSettingsHelper
    {
        public const string FriendlyNameAttributeName = "friendlyName";
        public const string UriAttributeName = "uri";
        public const string MsDataCollectorUri = "datacollector://Microsoft/CodeCoverage/2.0";
        public const string MsDataCollectorFriendlyName = "Code Coverage";
        public static bool IsMsDataCollector(XElement dataCollectorElement)
        {
            var friendlyNameAttribute = dataCollectorElement.Attribute(FriendlyNameAttributeName);
            if (friendlyNameAttribute != null)
            {
                return IsFriendlyMsCodeCoverage(friendlyNameAttribute.Value);
            }
            var uriAttribute = dataCollectorElement.Attribute(UriAttributeName);
            return uriAttribute != null && IsMsCodeCoverageUri(uriAttribute.Value);
        }

        public static bool IsFriendlyMsCodeCoverage(string friendlyName)
        {
            return friendlyName == "Code Coverage";
        }

        public static bool IsMsCodeCoverageUri(string uri)
        {
            return uri == "datacollector://Microsoft/CodeCoverage/2.0";
        }

        public static XElement FindMsDataCollector(XElement dataCollectors)
        {
            return dataCollectors.Elements().FirstOrDefault(IsMsDataCollector);
        }
    }
}
