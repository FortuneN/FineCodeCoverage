using FineCodeCoverage.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Core.Utilities
{
	public static class CoverageProjectUtil
	{
		public static bool IsDotNetSdkStyle(CoverageProject project)
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

		public static async Task<IEnumerable<ReferencedProject>> GetReferencedProjectsAsync(CoverageProject project)
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

				referencedProject.ProjectFileXElement = await XElementUtil.LoadAsync(referencedProject.ProjectFile, true);

				// HasExcludeFromCodeCoverageAssemblyAttribute

				referencedProject.HasExcludeFromCodeCoverageAssemblyAttribute = HasExcludeFromCodeCoverageAssemblyAttribute(referencedProject.ProjectFileXElement);

				// AssemblyName

				referencedProject.AssemblyName = GetAssemblyName(referencedProject.ProjectFileXElement, Path.GetFileNameWithoutExtension(referencedProject.ProjectFile));

				// add

				referencedProjects.Add(referencedProject);
			}

			return referencedProjects;
		}

		public static bool HasExcludeFromCodeCoverageAssemblyAttribute(XElement projectFileXElement)
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

		public static string GetAssemblyName(XElement projectFileXElement, string fallbackName = null)
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

		public static void ApplySettings(CoverageProject project, CoverageProjectSettings defaultSettings)
		{
			/*
			===================================
			Load default settings (if provided)
			===================================
			*/

			if (project.Settings == null)
			{
				project.Settings = new CoverageProjectSettings();
			}

			if (defaultSettings != null)
			{
				foreach (var property in defaultSettings.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
				{
					property.SetValue(project.Settings, property.GetValue(defaultSettings));
				}
			}

			/*
			===============================================================
			Override default settings with project's PropertyGroup settings
			===============================================================
			<PropertyGroup Label="FineCodeCoverage">
				...
			</PropertyGroup>
			*/

			static bool typeMatch(Type type, params Type[] otherTypes) => (otherTypes ?? new Type[0]).Any(ot => type == ot);

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

						if (typeMatch(property.PropertyType, typeof(string)))
						{
							property.SetValue(project.Settings, strValueArr.FirstOrDefault());
						}
						else if (typeMatch(property.PropertyType, typeof(string[])))
						{
							property.SetValue(project.Settings, strValueArr);
						}

						else if (typeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
						{
							if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
						{
							var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(int), typeof(int?)))
						{
							if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
						{
							var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(short), typeof(short?)))
						{
							if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
							{
								property.SetValue(project.Settings, vaue);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
						{
							var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(long), typeof(long?)))
						{
							if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
						{
							var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
						{
							if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
						{
							var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(double), typeof(double?)))
						{
							if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
						{
							var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(float), typeof(float?)))
						{
							if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
						{
							var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
							if (arr.Any()) property.SetValue(project.Settings, arr);
						}

						else if (typeMatch(property.PropertyType, typeof(char), typeof(char?)))
						{
							if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
							{
								property.SetValue(project.Settings, value);
							}
						}
						else if (typeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
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
