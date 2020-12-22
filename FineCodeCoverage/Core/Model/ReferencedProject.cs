using System.IO;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Model
{
	internal class ReferencedProject
	{
		public string ProjectFile { get; set; }
		public string AssemblyName { get; set; }
		public XElement ProjectFileXElement { get; internal set; }
		public string ProjectFolder => Path.GetDirectoryName(ProjectFile);
		public bool HasExcludeFromCodeCoverageAssemblyAttribute { get; set; }
	}
}
