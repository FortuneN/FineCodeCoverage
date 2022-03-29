using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal static class XPathNavigatorExtensions
    {
        public static bool HasChild(this XPathNavigator navigator, string elementName, string nsUri = "")
        {
            return navigator.Clone().MoveToChild(elementName, nsUri);
        }
    }
}
