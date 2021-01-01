using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Utilities
{
	public static class XElementUtil
	{
		public static XElement RemoveAllNamespaces(this XElement element)
		{
			return new XElement(element.Name.LocalName,
				from n in element.Nodes()
				select ((n is XElement) ? RemoveAllNamespaces(n as XElement) : n),
				element.HasAttributes ? (from a in element.Attributes() select a) : null);
		}

		public static async Task<XElement> LoadAsync(string path, bool removeNamespaces)
		{
			var xelement = XElement.Parse(await File.ReadAllTextAsync(path));

			if (removeNamespaces)
			{
				xelement = RemoveAllNamespaces(xelement);
			}

			return xelement;
		}
	}
}
