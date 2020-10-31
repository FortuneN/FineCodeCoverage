using System;
using System.IO;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
	internal class CoverageProject
	{
		public string ProjectGuid { get; set; }
		public string ProjectFolder => Path.GetDirectoryName(ProjectFile);
		public bool IsDotNetSdkStyle { get; set; }
		public string TestDllFileInOutputFolder { get; set; }
		public string WorkFolder { get; set; }
		public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFileInOutputFolder);
		public string FailureDescription { get; set; }
		public string FailureStage { get; set; }
		public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStage) || !string.IsNullOrWhiteSpace(FailureDescription);
		public string ProjectFile { get; set; }
		public string ProjectName { get; set; }
		public string CoverToolOutputFile { get; set; }
		public string TestDllFileInWorkFolder { get; set; }
		public AppOptions Settings { get; set; }
		public string WorkOutputFolder { get; set; }

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
