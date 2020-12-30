using System.Xml.Linq;

namespace FineCodeCoverage.Impl
{
    internal interface IXDocumentLoader
    {
		XDocument Load(string path);
    }
}
