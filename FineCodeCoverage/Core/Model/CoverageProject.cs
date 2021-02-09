using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine.Model
{
	internal interface ICoverageProjectFactory
    {
		CoverageProject Create();
    }

	[Export(typeof(ICoverageProjectFactory))]
    internal class CoverageProjectFactory : ICoverageProjectFactory
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ILogger logger;

        [ImportingConstructor]
		public CoverageProjectFactory(IAppOptionsProvider appOptionsProvider,IFileSynchronizationUtil fileSynchronizationUtil, ILogger logger)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.logger = logger;
        }
        public CoverageProject Create()
        {
			return new CoverageProject(appOptionsProvider,fileSynchronizationUtil, logger);
        }
    }


    internal class CoverageProject
	{
		private readonly IAppOptionsProvider appOptionsProvider;
		private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ILogger logger;
        private XElement projectFileXElement;
		private IAppOptions settings;
		private string fccPath;
		private string fccFolderName = "fine-code-coverage";
		private string buildOutputFolderName = "build-output";
		private string buildOutputPath;
		private string coverageToolOutputFolderName = "coverage-tool-output";

		public CoverageProject(IAppOptionsProvider appOptionsProvider,IFileSynchronizationUtil fileSynchronizationUtil,ILogger logger)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.logger = logger;
        }

		public bool IsDotNetSdkStyle(){
			return ProjectFileXElement
			.DescendantsAndSelf()
			.Where(x =>
			{
				//https://docs.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk?view=vs-2019

				/*
				<Project Sdk="My.Custom.Sdk">
					...
				</Project>
				<Project Sdk="My.Custom.Sdk/1.2.3">
					...
				</Project>
				*/
				if
				(
					x?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
					x?.Parent == null
				)
				{
					var sdkAttr = x?.Attributes()?.FirstOrDefault(attr => attr?.Name?.LocalName?.Equals("Sdk", StringComparison.OrdinalIgnoreCase) == true);

					if (sdkAttr?.Value?.Trim()?.StartsWith("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) == true)
					{
						return true;
					}
				}

				/*
				<Project>
					<Sdk Name="My.Custom.Sdk" Version="1.2.3" />
					...
				</Project>
				*/
				if
				(
					x?.Name?.LocalName?.Equals("Sdk", StringComparison.OrdinalIgnoreCase) == true &&
					x?.Parent?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
					x?.Parent?.Parent == null
				)
				{
					var nameAttr = x?.Attributes()?.FirstOrDefault(attr => attr?.Name?.LocalName?.Equals("Name", StringComparison.OrdinalIgnoreCase) == true);

					if (nameAttr?.Value?.Trim()?.StartsWith("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) == true)
					{
						return true;
					}
				}

				/*
				<Project>
					<PropertyGroup>
						<MyProperty>Value</MyProperty>
					</PropertyGroup>
					<Import Project="Sdk.props" Sdk="My.Custom.Sdk" />
						...
					<Import Project="Sdk.targets" Sdk="My.Custom.Sdk" />
				</Project>
				*/
				if
				(
					x?.Name?.LocalName?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true &&
					x?.Parent?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
					x?.Parent?.Parent == null
				)
				{
					var sdkAttr = x?.Attributes()?.FirstOrDefault(attr => attr?.Name?.LocalName?.Equals("Sdk", StringComparison.OrdinalIgnoreCase) == true);

					if (sdkAttr?.Value?.Trim()?.StartsWith("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) == true)
					{
						return true;
					}
				}

				return false;
			})
			.Any();
		}
		public string TestDllFile { get; set; }
		public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFile);
		public string FailureDescription { get; set; }
		public string FailureStage { get; set; }
		public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStage) || !string.IsNullOrWhiteSpace(FailureDescription);
		public string ProjectFile { get; set; }
		public string ProjectName { get; set; }
		public string CoverageOutputFile { get; set; }

		private bool TypeMatch(Type type, params Type[] otherTypes)
		{
			return (otherTypes ?? new Type[0]).Any(ot => type == ot);
		}
		
		
		public IAppOptions Settings
		{
			get
			{
				if(settings == null)
                {
					// get global settings

					settings = appOptionsProvider.Get();

					/*
					========================================
					Process PropertyGroup settings
					========================================
					<PropertyGroup Label="FineCodeCoverage">
						...
					</PropertyGroup>
					*/

					var settingsPropertyGroup = ProjectFileXElement.XPathSelectElement($"/PropertyGroup[@Label='{Vsix.Code}']");

					if (settingsPropertyGroup != null)
					{
						foreach (var property in settings.GetType().GetProperties())
						{
							try
							{
								var xproperty = settingsPropertyGroup.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

								if (xproperty == null)
								{
									continue;
								}

								var strValue = xproperty.Value;

								if (string.IsNullOrWhiteSpace(strValue))
								{
									continue;
								}

								var strValueArr = strValue.Split('\n', '\r').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

								if (!strValue.Any())
								{
									continue;
								}

								if (TypeMatch(property.PropertyType, typeof(string)))
								{
									property.SetValue(settings, strValueArr.FirstOrDefault());
								}
								else if (TypeMatch(property.PropertyType, typeof(string[])))
								{
									property.SetValue(settings, strValueArr);
								}

								else if (TypeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
								{
									if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
								{
									var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(int), typeof(int?)))
								{
									if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
								{
									var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(short), typeof(short?)))
								{
									if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
									{
										property.SetValue(settings, vaue);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
								{
									var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(long), typeof(long?)))
								{
									if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
								{
									var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
								{
									if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
								{
									var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(double), typeof(double?)))
								{
									if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
								{
									var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(float), typeof(float?)))
								{
									if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
								{
									var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else if (TypeMatch(property.PropertyType, typeof(char), typeof(char?)))
								{
									if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
									{
										property.SetValue(settings, value);
									}
								}
								else if (TypeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
								{
									var arr = strValueArr.Where(x => char.TryParse(x, out var _)).Select(x => char.Parse(x));
									if (arr.Any()) property.SetValue(settings, arr);
								}

								else
								{
									throw new Exception($"Cannot handle '{property.PropertyType.Name}' yet");
								}
							}
							catch (Exception exception)
							{
								logger.Log($"Failed to override '{property.Name}' setting", exception);
							}
						}
					}
				}
				return settings;
			}
		}
		public string CoverageOutputFolder { get; set; }
		

        public XElement ProjectFileXElement
		{
			get
			{
				if(projectFileXElement == null)
                {
					projectFileXElement = XElementUtil.Load(ProjectFile, true);
				}
				return projectFileXElement;

			}
		}
		public List<ReferencedProject> ReferencedProjects { get; set; }
		public string AssemblyName { get; set; }
		public bool Is64Bit { get; set; }
		public string RunSettingsFile { get; set; }

        public async Task<CoverageProject> StepAsync(string stepName, Func<CoverageProject, System.Threading.Tasks.Task> action)
		{
			if (HasFailed)
			{
				return this;
			}

			logger.Log($"{stepName} ({ProjectName})");

			try
			{
				await action(this);
			}
			catch (Exception exception)
			{
				FailureStage = stepName;
				FailureDescription = exception.ToString();
			}

			if (HasFailed)
			{
				logger.Log($"{stepName} ({ProjectName}) Failed", FailureDescription);
			}
			
			return this;
		}

		internal async System.Threading.Tasks.Task PrepareForCoverageAsync(DTE dte)
        {
			SetPaths();
			EnsureDirectories();
			CleanDirectory();
			SynchronizeBuildOutput();
			await SetAssemblyNameAndReferencedProjectsAsync(dte);
        }

		private async System.Threading.Tasks.Task SetAssemblyNameAndReferencedProjectsAsync(DTE dte)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var project = dte.Solution.Projects.Cast<Project>().First(p =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				//have to try here as unloaded projects will throw
				var projectFullName = "";
				try
				{
					projectFullName = p.FullName;
				}
				catch { }
				return projectFullName == ProjectFile;
			});
			AssemblyName = (string)project.Properties.Item("AssemblyName").Value;
			var vsproject = project.Object as VSLangProj.VSProject;
			ReferencedProjects = vsproject.References.Cast<VSLangProj.Reference>().Where(r => r.SourceProject != null).Select(r=>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				return new ReferencedProject(r.SourceProject.FullName, r.Path);
			}).ToList();
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
			fileSynchronizationUtil.Synchronize(ProjectOutputFolder, buildOutputPath,fccFolderName);
			TestDllFile = Path.Combine(buildOutputPath, Path.GetFileName(TestDllFile));
		}

	}
}
