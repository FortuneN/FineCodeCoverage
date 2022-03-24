using System.Xml.XPath;
using System.IO;
using System.Xml;
using System.Globalization;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal static class XmlHelper
    {
        public const string XmlDeclaration = "<?xml version='1.0' encoding='utf-8'?>";

        public static IXPathNavigable CreateXPathNavigable(string contents)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(XmlDeclaration + contents);
            return xmlDocument.CreateNavigator();
        }

        public static string DumpXmlContents(this IXPathNavigable xmlPathNavigable)
        {
            var navigator = xmlPathNavigable.CreateNavigator();
            navigator.MoveToRoot();
            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                navigator.WriteSubtree(new XmlTextWriter((TextWriter)stringWriter)
                {
                    Formatting = Formatting.Indented
                });
                return stringWriter.ToString();
            }
        }
    }
}
