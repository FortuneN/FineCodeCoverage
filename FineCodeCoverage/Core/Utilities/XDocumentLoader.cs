using System.ComponentModel.Composition;
using System.Xml.Linq;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IXDocumentLoader))]
    internal class XDocumentLoader : IXDocumentLoader
    {
        public XDocument Load(string path)
        {
			return XDocument.Load(path);
        }
    }
}
