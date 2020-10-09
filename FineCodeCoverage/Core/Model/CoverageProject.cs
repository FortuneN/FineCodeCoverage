using System;
using System.IO;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
	internal class CoverageProject
	{
		public string ProjectFolder { get; set; }
		public bool IsDotNetCore { get; internal set; }
		public string TestDllFileInOutputFolder { get; internal set; }
		public string WorkFolder { get; internal set; }
		public string ProjectOutputFolder { get; internal set; }
		public string FailureDescription { get; internal set; }
		public string FailureStage { get; internal set; }
		public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStage) || !string.IsNullOrWhiteSpace(FailureDescription);
		public string ProjectFile { get; internal set; }
		public string ProjectName => Path.GetFileNameWithoutExtension(ProjectFile);
		public string CoverOutputFile { get; internal set; }
		public string TestDllFileInWorkFolder { get; internal set; }
		public AppSettings Settings { get; internal set; }
		public string WorkOutputFolder { get; internal set; }
		public string ProjectFileXml { get; internal set; }

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
