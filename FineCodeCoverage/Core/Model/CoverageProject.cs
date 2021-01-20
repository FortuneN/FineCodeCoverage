using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
	internal class CoverageProject
	{
		private string fccPath;
		private string fccFolderName = "fine-code-coverage";
		private string buildOutputFolderName = "build-output";
		private string buildOutputPath;
		private string coverageToolOutputFolderName = "coverage-tool-output";

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

		internal void PrepareForCoverage()
        {
			SetPaths();
			EnsureDirectories();
			CleanDirectory();
			SynchronizeBuildOutput();
        }

		private void SetPaths()
        {
			fccPath = Path.Combine(ProjectOutputFolder, fccFolderName);
			buildOutputPath = Path.Combine(fccPath, buildOutputFolderName);
			CoverageOutputFolder = Path.Combine(fccPath, coverageToolOutputFolderName);
			CoverageOutputFile = Path.Combine(CoverageOutputFolder, "project.coverage.xml");
		}
		private void EnsureDirectories()
        {
			EnsureFccDirectory();
			EnsureBuildOutputDirectory();
			EnsureEmptyOutputFolder();
		}
		private void EnsureFccDirectory()
        {
			CreateIfDoesNotExist(fccPath);
		}
		private void EnsureBuildOutputDirectory()
        {
			CreateIfDoesNotExist(buildOutputPath);
		}
		private void CreateIfDoesNotExist(string path)
        {
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}
		/// <summary>
		/// Delete all files and sub-directories from the output folder if it exists, or creates the directory if it does not exist.
		/// </summary>
		private void EnsureEmptyOutputFolder()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(CoverageOutputFolder);
			if (directoryInfo.Exists)
			{
				foreach (FileInfo file in directoryInfo.GetFiles())
				{
					file.TryDelete();
				}
				foreach (DirectoryInfo subDir in directoryInfo.GetDirectories())
				{
					subDir.TryDelete(true);
				}
			}
			else
			{
				Directory.CreateDirectory(CoverageOutputFolder);
			}
		}
		private void CleanDirectory()
        {
			var exclusions = new List<string>{ buildOutputFolderName, coverageToolOutputFolderName};
			var fccDirectory = new DirectoryInfo(fccPath);

			fccDirectory.EnumerateFileSystemInfos().AsParallel().ForAll(fileOrDirectory =>
			   {
				   if (!exclusions.Contains(fileOrDirectory.Name))
				   {
					   try
					   {
						   if (fileOrDirectory is FileInfo)
						   {
							   fileOrDirectory.Delete();
						   }
						   else
						   {
							   (fileOrDirectory as DirectoryInfo).Delete(true);
						   }
					   }
					   catch (Exception) { }
				   }
			   });
            
        }
		private void SynchronizeBuildOutput()
		{
			var logs = FileSynchronizationUtil.Synchronize(ProjectOutputFolder, buildOutputPath,fccFolderName);
			logs.ForEach(l => Logger.Log(l));
			TestDllFile = Path.Combine(buildOutputPath, Path.GetFileName(TestDllFile));
		}

	}
}
