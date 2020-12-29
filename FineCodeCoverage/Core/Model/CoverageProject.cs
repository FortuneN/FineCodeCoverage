using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Engine.Utilities;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
	internal class CoverageProject
	{
		public string ProjectGuid { get; set; }
		public string ProjectFolder => Path.GetDirectoryName(ProjectFile);
		public bool IsDotNetSdkStyle { get; set; }
		public string TestDllFile { get; set; }
		public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFile);
		public string FailureDescription { get; set; }
		public string FailureStage { get; set; }
		public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStage) || !string.IsNullOrWhiteSpace(FailureDescription);
		public string ProjectFile { get; set; }
		public string ProjectName { get; set; }
		public string CoverageOutputFile { get; set; }
		public AppOptions Settings { get; set; }
		public string CoverageOutputFolder { get; set; }
		public XElement ProjectFileXElement { get; set; }
		public List<ReferencedProject> ReferencedProjects { get; set; }
		public bool HasExcludeFromCodeCoverageAssemblyAttribute { get; set; }
		public string AssemblyName { get; set; }
		public bool Is64Bit { get; set; }
		public string RunSettingsFile { get; set; }
        public string CoverletCoberturaFile { get; internal set; }

        public CoverageProject Step(string stepName, Action<CoverageProject> action)
		{
			if (HasFailed)
			{
				return this;
			}

			Logger.Log($"{stepName} ({ProjectName})");

			try
			{
				action(this);
			}
			catch (Exception exception)
			{
				FailureStage = stepName;
				FailureDescription = exception.ToString();
			}

			if (HasFailed)
			{
				Logger.Log($"{stepName} ({ProjectName}) Failed", FailureDescription);
			}
			
			return this;
		}
	}
}
