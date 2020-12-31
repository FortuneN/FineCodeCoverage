using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using FineCodeCoverage.Core.Model;
using System.Collections.Concurrent;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Cobertura;
using FineCodeCoverage.Core.OpenCover;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.MsTestPlatform;
using FineCodeCoverage.Core.ReportGenerator;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

namespace FineCodeCoverage.Core
{
	public static class FCCEngine
	{
		private static string AppDataFolder { get; set; }
		private static string[] ProjectExtensions { get; } = new string[] { ".csproj", ".vbproj" };
		private static ConcurrentDictionary<string, string> ProjectFoldersCache { get; } = new ConcurrentDictionary<string, string>();

		public static void Initialize()
		{
			AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.Code);
			Directory.CreateDirectory(AppDataFolder);
			
			CleanupLegacyFolders();

			CoverletUtil.Initialize(AppDataFolder);
			ReportGeneratorUtil.Initialize(AppDataFolder);
			MsTestPlatformUtil.Initialize(AppDataFolder);
			OpenCoverUtil.Initialize(AppDataFolder);
		}

		private static void CleanupLegacyFolders()
		{
			Directory
			.GetDirectories(AppDataFolder, "*", SearchOption.TopDirectoryOnly)
			.Where(path =>
			{
				var name = Path.GetFileName(path);

				if (name.Contains("__"))
				{
					return true;
				}

				if (Guid.TryParse(name, out var _))
				{
					return true;
				}

				return false;
			})
			.ToList()
			.ForEach(path =>
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch
				{
					// ignore
				}
			});
		}

		//public static IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber)
		//{
		//	return CoverageLines
		//	.AsParallel()
		//	.Where(x => x.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase))
		//	.Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
		//	.ToArray();
		//}

		//public static string[] GetSourceFiles(string assemblyName, string qualifiedClassName)
		//{
		//	// Note : There may be more than one file; e.g. in the case of partial classes

		//	var package = CoverageReport
		//		.Packages.Package
		//		.SingleOrDefault(x => x.Name.Equals(assemblyName));

		//	if (package == null)
		//	{
		//		return new string[0];
		//	}

		//	var classFiles = package
		//		.Classes.Class
		//		.Where(x => x.Name.Equals(qualifiedClassName))
		//		.Select(x => x.Filename)
		//		.ToArray();

		//	return classFiles;
		//}

		private static bool IsDotNetSdkStyle(CoverageProject project)
		{
			return project.ProjectFileXElement
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
		
		public static void ClearProcesses()
		{
			ProcessUtil.ClearProcesses();
		}

		public static async Task<CalculateCoverageResponse> CalculateCoverageAsync(CalculateCoverageRequest request)
		{
			var response = new CalculateCoverageResponse();

			// reset

			ClearProcesses();

			// process pipeline

			request
			.Projects
			.Select(project =>
			{
				using var msbWorkspace = MSBuildWorkspace.Create();
				var msbProject = msbWorkspace.OpenProjectAsync(project.ProjectFile).GetAwaiter().GetResult();
				
				project.ProjectName = msbProject.Name;
				project.ProjectFileXElement = XElementUtil.Load(project.ProjectFile, true);
				
				ApplySettings(project);

				if (!project.Settings.Enabled)
				{
					project.FailureDescription = $"Disabled";
					return project;
				}

				project.Is64Bit = msbProject.CompilationOptions.Platform == Platform.X64;
				project.CoverageOutputFolder = Path.Combine(project.ProjectOutputFolder, "fine-code-coverage");
				project.CoverageOutputFile = Path.Combine(project.CoverageOutputFolder, "project.coverage.xml");
				project.IsDotNetSdkStyle = IsDotNetSdkStyle(project);
				project.ReferencedProjects = GetReferencedProjects(project); //TODO:msbProject.ProjectReferences
				project.HasExcludeFromCodeCoverageAssemblyAttribute = HasExcludeFromCodeCoverageAssemblyAttribute(project.ProjectFileXElement);
				project.AssemblyName = string.IsNullOrWhiteSpace(msbProject.AssemblyName) ? GetAssemblyName(project.ProjectFileXElement, Path.GetFileNameWithoutExtension(project.ProjectFile)) : msbProject.AssemblyName;
				
				if (!Directory.Exists(project.CoverageOutputFolder))
				{
					Directory.CreateDirectory(project.CoverageOutputFolder);
				}

				try
				{
					var legacyOutputFolder = Path.Combine(project.ProjectOutputFolder, "_outputFolder");
					Directory.Delete(legacyOutputFolder, true);
				}
				catch
				{
					// ignore
				}

				try
				{
					var defaultOutputFolder = Path.GetDirectoryName(project.ProjectOutputFolder);
					var legacyWorkFolder = Path.Combine(defaultOutputFolder, "fine-code-coverage");
					Directory.Delete(legacyWorkFolder, true);
				}
				catch
				{
					// ignore
				}

				return project;
			})
			.Select(p => p.Step("Run Coverage Tool", project =>
			{
				// run the appropriate cover tool

				if (project.IsDotNetSdkStyle)
				{
					CoverletUtil.RunCoverlet(project, true);
				}
				else
				{
					OpenCoverUtil.RunOpenCover(project, true);
				}
			}))
			.Where(x => !x.HasFailed)
			.ToArray();

			// project files

			var coverOutputFiles = request.Projects
				.Select(x => x.CoverageOutputFile)
				.ToArray();

			if (!coverOutputFiles.Any())
			{
				return response;
			}

			// run reportGenerator process

			ReportGeneratorUtil.RunReportGenerator(coverOutputFiles, request.DarkMode, out var unifiedHtmlFile, out var unifiedXmlFile, true);

			// update CoverageLines

			response.CoverageReport = CoberturaUtil.ProcessCoberturaXmlFile(unifiedXmlFile, out var coverageLines);
			response.CoverageLines = coverageLines;

			// update HtmlFilePath

			ReportGeneratorUtil.ProcessUnifiedHtmlFile(unifiedHtmlFile, request.DarkMode, out var coverageHtmlFile);
			response.HtmlContent = await File.ReadAllTextAsync(coverageHtmlFile);

			// return

			return response;
		}

		private static List<ReferencedProject> GetReferencedProjects(CoverageProject project)
		{
			/*
			<ItemGroup>
				<ProjectReference Include="..\BranchCoverage\Branch_Coverage.csproj" />
				<ProjectReference Include="..\FxClassLibrary1\FxClassLibrary1.csproj"></ProjectReference>
			</ItemGroup>
			 */

			var referencedProjects = new List<ReferencedProject>();

			var xprojectReferences = project.ProjectFileXElement.XPathSelectElements($"/ItemGroup/ProjectReference[@Include]");
			
			foreach (var xprojectReference in xprojectReferences)
			{
				var referencedProject = new ReferencedProject();

				// ProjectFile

				referencedProject.ProjectFile = xprojectReference.Attribute("Include").Value;

				if (!Path.IsPathRooted(referencedProject.ProjectFile))
				{
					referencedProject.ProjectFile = Path.GetFullPath(Path.Combine(project.ProjectFolder, referencedProject.ProjectFile));
				}

				// ProjectFileXElement

				referencedProject.ProjectFileXElement = XElementUtil.Load(referencedProject.ProjectFile, true);

				// HasExcludeFromCodeCoverageAssemblyAttribute
				
				referencedProject.HasExcludeFromCodeCoverageAssemblyAttribute = HasExcludeFromCodeCoverageAssemblyAttribute(referencedProject.ProjectFileXElement);

				// AssemblyName

				referencedProject.AssemblyName = GetAssemblyName(referencedProject.ProjectFileXElement, Path.GetFileNameWithoutExtension(referencedProject.ProjectFile));

				// add

				referencedProjects.Add(referencedProject);
			}
			
			return referencedProjects;
		}

		private static bool HasExcludeFromCodeCoverageAssemblyAttribute(XElement projectFileXElement)
		{
			/*
			 ...
			<ItemGroup>
				<AssemblyAttribute Include="System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" />
			</ItemGroup>
			...
			 */

			var xassemblyAttribute = projectFileXElement.XPathSelectElement($"/ItemGroup/AssemblyAttribute[@Include='System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute']");
			
			return xassemblyAttribute != null;
		}

		private static string GetAssemblyName(XElement projectFileXElement, string fallbackName = null)
		{
			/*
			<PropertyGroup>
				...
				<AssemblyName>Branch_Coverage.Tests</AssemblyName>
				...
			</PropertyGroup>
			 */

			var xassemblyName = projectFileXElement.XPathSelectElement("/PropertyGroup/AssemblyName");

			var result = xassemblyName?.Value?.Trim();

			if (string.IsNullOrWhiteSpace(result))
			{
				result = fallbackName;
			}

			return result;
		}


		private static bool TypeMatch(Type type, params Type[] otherTypes)
		{
			return (otherTypes ?? new Type[0]).Any(ot => type == ot);
		}

		private static void ApplySettings(CoverageProject project)
		{
			// get global settings

			if (project.Settings == null)
			{
				project.Settings = new AppOptions();
			}

			/*
			========================================
			Process PropertyGroup settings
			========================================
			<PropertyGroup Label="FineCodeCoverage">
				...
			</PropertyGroup>
			*/

			var settingsPropertyGroup = project.ProjectFileXElement.XPathSelectElement($"/PropertyGroup[@Label='{Constants.Code}']");

			if (settingsPropertyGroup != null)
			{
				foreach (var property in project.Settings.GetType().GetProperties())
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
							property.SetValue(project.Settings, strValueArr.FirstOrDefault());
						}
						else if (TypeMatch(property.PropertyType, typeof(string[])))
						{
							property.SetValue(project.Settings, strValueArr);
						}

						else if (TypeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
						{
							if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
						{
							var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(int), typeof(int?)))
						{
							if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
						{
							var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(short), typeof(short?)))
						{
							if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
							{
								property.SetValue(project.Settings, vaue);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
						{
							var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(long), typeof(long?)))
						{
							if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
						{
							var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
						{
							if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
						{
							var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(double), typeof(double?)))
						{
							if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
						{
							var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(float), typeof(float?)))
						{
							if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
						{
							var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (TypeMatch(property.PropertyType, typeof(char), typeof(char?)))
						{
							if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (TypeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
						{
							var arr = strValueArr.Where(x => char.TryParse(x, out var _)).Select(x => char.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else
						{
							throw new Exception($"Cannot handle '{property.PropertyType.Name}' yet");
						}
					}
					catch (Exception exception)
					{
						Logger.Log($"Failed to override '{property.Name}' setting", exception);
					}
				}
			}
		}
	}
}