using System.ComponentModel.Composition;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IXmlUtils))]
    internal class XmlUtils : IXmlUtils
    {
        public XElement Load(string path)
        {
            return XElement.Load(path);
        }

        public string Serialize(XElement xmlElement)
        {
            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xmlElement).ToString();
        }
    }
}
