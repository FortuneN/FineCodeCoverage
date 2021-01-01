using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.Model
{
	public class CoverageProject
	{
		public string RequestId { get; set; }

		public string ProjectName { get; set; }

		public string ProjectFile { get; set; }

		public string ProjectFolder => Path.GetDirectoryName(ProjectFile);
		
		public bool IsDotNetSdkStyle { get; set; }
		
		public string TestDllFile { get; set; }
		
		public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFile);
		
		public string CoverageOutputFile { get; set; }
		
		public string CoverageOutputFolder { get; set; }

		public bool HasExcludeFromCodeCoverageAssemblyAttribute { get; set; }
		
		public string AssemblyName { get; set; }
		
		public bool Is64Bit { get; set; }
		
		public string RunSettingsFile { get; set; }

		public string Error { get; set; }

		[JsonIgnore]
		public XElement ProjectFileXElement { get; set; }

		[JsonIgnore]
		public IEnumerable<ReferencedProject> ReferencedProjects { get; set; }

		[JsonIgnore]
		public CoverageProjectSettings Settings { get; set; }
	}
}
