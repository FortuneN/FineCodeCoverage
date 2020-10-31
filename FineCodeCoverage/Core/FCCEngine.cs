using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine
{
	internal static class FCCEngine
	{
		public static string HtmlFilePath { get; private set; }
		public static List<CoverageLine> CoverageLines { get; private set; } = new List<CoverageLine>();

		public static string AppDataFolder { get; private set; }
		public static string[] ProjectExtensions { get; } = new string[] { ".csproj", ".vbproj" };
		public static ConcurrentDictionary<string, string> ProjectFoldersCache { get; } = new ConcurrentDictionary<string, string>();
		
		public static void Initialize()
		{
			AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
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

		public static CoverageLine GetLine(string filePath, int lineNumber)
		{
			return CoverageLines
				.AsParallel()
				.SingleOrDefault(line =>
				{
					return line.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase)
						&& line.Line.Number == lineNumber;
				});
		}

		private static AppOptions GetSettings(CoverageProject project)
		{
			// get global settings

			var settings = AppOptions.Get();

			// override with test project settings

			XElement xproject;

			try
			{
				xproject = XElement.Parse(File.ReadAllText(project.ProjectFile));
			}
			catch (Exception ex)
			{
				Logger.Log("Failed to parse project file", ex);
				return settings;
			}

			var xsettings = xproject.Descendants("PropertyGroup").FirstOrDefault(x =>
			{
				var label = x.Attribute("Label")?.Value?.Trim() ?? string.Empty;

				if (!Vsix.Code.Equals(label, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				return true;
			});

			if (xsettings == null)
			{
				return settings;
			}

			foreach (var property in settings.GetType().GetProperties())
			{
				try
				{
					var xproperty = xsettings.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

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
					Logger.Log($"Failed to override '{property.Name}' setting", exception);
				}
			}

			// return

			return settings;
		}

		public static string GetSourceFileNameFromReportGeneratorHtmlFileName(string htmlFileNameWithoutExtension)
		{
			var html_file_tokens = htmlFileNameWithoutExtension.Split('_');
			var html_file_package = html_file_tokens.First();
			var html_file_class = $".{html_file_tokens.Last()}";

			var cs_file_name = CoverageLines
				.AsParallel()
				.Where(x => x.Package.Name.Equals(html_file_package, StringComparison.OrdinalIgnoreCase))
				.Where(x => x.Class.Name.EndsWith(html_file_class, StringComparison.OrdinalIgnoreCase))
				.Where(x => !string.IsNullOrWhiteSpace(x.Class.Filename))
				.Select(x => x.Class.Filename)
				.FirstOrDefault();

			return cs_file_name;
		}

		private static bool IsDotNetSdkStyle(CoverageProject project)
		{
			return XElement
			.Parse(File.ReadAllText(project.ProjectFile))
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
		
		private static bool TypeMatch(Type type, params Type[] otherTypes)
		{
			return (otherTypes ?? new Type[0]).Any(ot => type == ot);
		}

		public static void ReloadCoverage(IEnumerable<CoverageProject> projects, bool darkMode, Action<Exception> marginHighlightsCallback, Action<Exception> outputWindowCallback, Action<Exception> doneCallback)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					// reset

					CoverageLines.Clear();
					HtmlFilePath = default;

					// process pipeline

					projects = projects
					.AsParallel()
					.Select(project =>
					{
						project.Settings = GetSettings(project);

						if (!project.Settings.Enabled)
						{
							project.FailureDescription = $"Disabled";
							return project;
						}

						if (string.IsNullOrWhiteSpace(project.ProjectFile))
						{
							project.FailureDescription = $"Unsupported project type for DLL '{project.TestDllFileInOutputFolder}'";
							return project;
						}

						project.WorkOutputFolder = Path.Combine(project.WorkFolder, "_outputfolder");
						project.CoverToolOutputFile = Path.Combine(project.WorkOutputFolder, "_cover.tool.xml");
						project.TestDllFileInWorkFolder = Path.Combine(project.WorkFolder, Path.GetFileName(project.TestDllFileInOutputFolder));
						project.IsDotNetSdkStyle = IsDotNetSdkStyle(project);

						return project;
					})
					.Select(p => p.Step("Ensure Work Folder Exists", project =>
					{
						// create folders

						Directory.CreateDirectory(project.WorkFolder);
						Directory.CreateDirectory(project.WorkOutputFolder);
					}))
					.Select(p => p.Step("Synchronize Output Files To Work Folder", project =>
					{
						// sync files from output folder to work folder where we do the analysis

						FileSynchronizationUtil.Synchronize(project.ProjectOutputFolder, project.WorkFolder);
					}))
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

					var coverOutputFiles = projects
						.AsParallel()
						.Select(x => x.CoverToolOutputFile)
						.ToArray();

					if (!coverOutputFiles.Any())
					{
						marginHighlightsCallback?.Invoke(default);
						outputWindowCallback?.Invoke(default);
						doneCallback?.Invoke(default);
						return;
					}

					// run reportGenerator process

					ReportGeneratorUtil.RunReportGenerator(coverOutputFiles, darkMode, out var unifiedHtmlFile, out var unifiedXmlFile, true);

					// finalize

					Parallel.Invoke
					(
						() =>
						{
							try
							{
								CoberturaUtil.ProcessCoberturaXmlFile(unifiedXmlFile, out var coverageLines);

								CoverageLines = coverageLines;

								marginHighlightsCallback?.Invoke(default);
							}
							catch (Exception exception)
							{
								marginHighlightsCallback?.Invoke(exception);
							}
						},
						() =>
						{
							try
							{
								ReportGeneratorUtil.ProcessCoberturaHtmlFile(unifiedHtmlFile, darkMode, out var coverageHtml);

								HtmlFilePath = coverageHtml;

								outputWindowCallback?.Invoke(default);
							}
							catch (Exception exception)
							{
								outputWindowCallback?.Invoke(exception);
							}
						}
					);

					doneCallback?.Invoke(default);
				}
				catch (Exception exception)
				{
					doneCallback?.Invoke(exception);
				}
			});
		}
	}
}