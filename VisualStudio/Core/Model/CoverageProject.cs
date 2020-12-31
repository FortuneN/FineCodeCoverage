namespace FineCodeCoverage.Core.Model
{
	public class CoverageProject
	{
		//public string ProjectGuid { get; set; }
		//public string ProjectFolder => Path.GetDirectoryName(ProjectFile);
		//public bool IsDotNetSdkStyle { get; set; }
		public string TestDllFile { get; set; }
		//public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFile);
		//public string FailureDescription { get; set; }
		//public string FailureStep { get; set; }
		//public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStep) || !string.IsNullOrWhiteSpace(FailureDescription);
		public string ProjectFile { get; set; }
		//public string ProjectName { get; set; }
		//public string CoverageOutputFile { get; set; }
		//public AppOptions Settings { get; set; }
		//public string CoverageOutputFolder { get; set; }
		//public XElement ProjectFileXElement { get; set; }
		//public List<ReferencedProject> ReferencedProjects { get; set; }
		//public bool HasExcludeFromCodeCoverageAssemblyAttribute { get; set; }
		//public string AssemblyName { get; set; }
		//public bool Is64Bit { get; set; }
		public string RunSettingsFile { get; set; }
	}
}
