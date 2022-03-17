using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Utilities
{
	public static class LinqToXmlUtil
	{
		private class Utf8StringWriter : StringWriter
		{
			public Utf8StringWriter(StringBuilder sb) : base(sb) { }
			public override Encoding Encoding { get { return Encoding.UTF8; } }
		}

		public static string ToXmlString(this XDocument xdoc)
		{
			return xdoc.Declaration == null ? xdoc.ToString() : xdoc.Declaration + Environment.NewLine + xdoc.ToString();
		}

		public static string FormatXml(this XDocument xDocument,bool utf8 = true)
		{
			StringBuilder builder = new StringBuilder();
			StringWriter writer = utf8 ? new Utf8StringWriter(builder) : new StringWriter(builder);

			xDocument.Save(writer, SaveOptions.None);
			return builder.ToString();
		}

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

		public static XElement GetStrictDescendant(this XContainer element, string path)
		{
			var names = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (names.Length == 0)
				throw new ArgumentException("Empty path", nameof(path));

			var result = element;
			foreach (var name in names)
			{
				result = result.Element(name);
				if (result == null)
				{
					return null;
				}
			}
			return (XElement)result;
		}
	}
}
