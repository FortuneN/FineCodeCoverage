using System.Xml.Linq;

namespace FineCodeCoverage.Core.Utilities
{
    interface IXmlUtils
    {
        XElement Load(string path);
        string Serialize(XElement xmlElement);
    }
}
