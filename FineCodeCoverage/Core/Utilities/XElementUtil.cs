using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Utilities
{
	public static class XElementUtil
	{
		public static XElement RemoveAllNamespaces(this XElement @this)
		{
			return new XElement(@this.Name.LocalName,
				from n in @this.Nodes()
				select ((n is XElement) ? RemoveAllNamespaces(n as XElement) : n),
				@this.HasAttributes ? (from a in @this.Attributes() select a) : null);
		}

		public static XElement Load(string path, bool removeNamespaces)
		{
			var xelement = XElement.Parse(File.ReadAllText(path));

			if (removeNamespaces)
			{
				xelement = xelement.RemoveAllNamespaces();
			}

			return xelement;
		}
	}
}
