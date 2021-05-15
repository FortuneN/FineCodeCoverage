using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.MSBuild;

namespace FineCodeCoverage.Engine.Model
{
    internal class CoverageProject : ICoverageProject
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ILogger logger;
        private readonly DTE dte;
        private readonly bool canUseMsBuildWorkspace;
        private XElement projectFileXElement;
        private IAppOptions settings;
        private readonly string fccFolderName = "fine-code-coverage";
        private readonly string buildOutputFolderName = "build-output";
        private string BuildOutputPath => Path.Combine(FCCOutputFolder, buildOutputFolderName);
        private readonly string coverageToolOutputFolderName = "coverage-tool-output";

        public CoverageProject(IAppOptionsProvider appOptionsProvider, IFileSynchronizationUtil fileSynchronizationUtil, ILogger logger, DTE dte, bool canUseMsBuildWorkspace)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.logger = logger;
            this.dte = dte;
            this.canUseMsBuildWorkspace = canUseMsBuildWorkspace;
        }

        public string AllProjectsCoverageOutputFolder => ProjectFileXElement.XPathSelectElement($"/PropertyGroup/AllProjectsCoverageOutputFolder")?.Value;
        
        public string FCCOutputFolder => Path.Combine(ProjectOutputFolder, fccFolderName);
        public bool IsDotNetSdkStyle()
        {
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
        public string CoverageOutputFile => Path.Combine(CoverageOutputFolder, $"{ProjectName}.coverage.xml");

        private bool TypeMatch(Type type, params Type[] otherTypes)
        {
            return (otherTypes ?? new Type[0]).Any(ot => type == ot);
        }


        public IAppOptions Settings
        {
            get
            {
                if (settings == null)
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
                if (projectFileXElement == null)
                {
                    projectFileXElement = XElementUtil.Load(ProjectFile, true);
                }
                return projectFileXElement;

            }
        }
        public List<string> ExcludedReferencedProjects { get; } = new List<string>();
        public bool Is64Bit { get; set; }
        public string RunSettingsFile { get; set; }

        public async System.Threading.Tasks.Task StepAsync(string stepName, Func<ICoverageProject, System.Threading.Tasks.Task> action)
        {
            if (HasFailed)
            {
                return;
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
        }

        public async System.Threading.Tasks.Task PrepareForCoverageAsync()
        {
            EnsureDirectories();
            CleanDirectory();
            SynchronizeBuildOutput();
            await SetExcludedReferencedProjectsAsync();
        }

        private async System.Threading.Tasks.Task SetExcludedReferencedProjectsAsync()
        {
            List<ReferencedProject> referencedProjects = await SafeGetReferencedProjectsFromDteAsync();

            if (referencedProjects == null)
            {
                referencedProjects = await GetReferencedProjectsFromProjectFileAsync();
            }
            foreach (var referencedProject in referencedProjects)
            {
                if (referencedProject.ExcludeFromCodeCoverage)
                {
                    ExcludedReferencedProjects.Add(referencedProject.AssemblyName);
                }
            }
        }
        private async Task<List<ReferencedProject>> SafeGetReferencedProjectsFromDteAsync()
        {
            try
            {
                return await GetReferencedProjectsFromDteAsync();
            }
            catch (Exception) { }
            return null;
        }
        private async Task<List<ReferencedProject>> GetReferencedProjectsFromDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var project = dte.Solution.Projects.Cast<Project>().FirstOrDefault(p =>
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

            if (project == null)
            {
                return null;
            }

            var vsproject = project.Object as VSLangProj.VSProject;
            return vsproject.References.Cast<VSLangProj.Reference>().Where(r => r.SourceProject != null).Select(r =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var assemblyName = Path.GetFileNameWithoutExtension(r.Path);
                return new ReferencedProject(r.SourceProject.FullName, assemblyName);
            }).ToList();

        }

        private async Task<List<ReferencedProject>> SafeGetReferencedProjectsWithDesignTimeBuildAsync()
        {
            try
            {
                return await GetReferencedProjectsWithDesignTimeBuildWorkerAsync();
            }
            catch (Exception exception)
            {
                logger.Log("Unable to get referenced projects with design time build", exception);
            }
            return new List<ReferencedProject>();
        }
        private async Task<List<ReferencedProject>> GetReferencedProjectsWithDesignTimeBuildWorkerAsync()
        {
            var msBuildWorkspace = MSBuildWorkspace.Create();
            var project = await msBuildWorkspace.OpenProjectAsync(ProjectFile);
            var solution = msBuildWorkspace.CurrentSolution;
            return project.ProjectReferences.Select(
                pr => solution.Projects.First(p => p.Id == pr.ProjectId).FilePath)
                    .Where(path => path != null)
                    .Select(path => new ReferencedProject(path)).ToList();
        }


        private async Task<List<ReferencedProject>> GetReferencedProjectsFromProjectFileAsync()
        {
            /*
			<ItemGroup>
				<ProjectReference Include="..\BranchCoverage\Branch_Coverage.csproj" />
				<ProjectReference Include="..\FxClassLibrary1\FxClassLibrary1.csproj"></ProjectReference>
			</ItemGroup>
			 */


            var xprojectReferences = ProjectFileXElement.XPathSelectElements($"/ItemGroup/ProjectReference[@Include]");
            var requiresDesignTimeBuild = false;
            List<string> referencedProjectFiles = new List<string>();
            foreach (var xprojectReference in xprojectReferences)
            {
                var referencedProjectProjectFile = xprojectReference.Attribute("Include").Value;
                if (referencedProjectProjectFile.Contains("$("))
                {
                    if (canUseMsBuildWorkspace)
                    {
                        requiresDesignTimeBuild = true;
                        break;
                    }
                    else
                    {
                        logger.Log($"Cannot exclude referenced project {referencedProjectProjectFile} of {ProjectFile} with {ReferencedProject.excludeFromCodeCoveragePropertyName}.  Cannot use MSBuildWorkspace");
                    }
                    
                }
                else
                {
                    if (!Path.IsPathRooted(referencedProjectProjectFile))
                    {
                        referencedProjectProjectFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ProjectFile), referencedProjectProjectFile));
                    }
                    referencedProjectFiles.Add(referencedProjectProjectFile);
                }

            }

            if (requiresDesignTimeBuild)
            {
                var referencedProjects = await SafeGetReferencedProjectsWithDesignTimeBuildAsync();
                return referencedProjects;

            }

            return referencedProjectFiles.Select(referencedProjectProjectFile => new ReferencedProject(referencedProjectProjectFile)).ToList();
        }

        private void EnsureDirectories()
        {
            EnsureFccDirectory();
            EnsureBuildOutputDirectory();
            EnsureEmptyOutputFolder();
        }
        private void EnsureFccDirectory()
        {
            CreateIfDoesNotExist(FCCOutputFolder);
        }
        private void EnsureBuildOutputDirectory()
        {
            CreateIfDoesNotExist(BuildOutputPath);
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
            var exclusions = new List<string> { buildOutputFolderName, coverageToolOutputFolderName };
            var fccDirectory = new DirectoryInfo(FCCOutputFolder);

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
            fileSynchronizationUtil.Synchronize(ProjectOutputFolder, BuildOutputPath, fccFolderName);
            TestDllFile = Path.Combine(BuildOutputPath, Path.GetFileName(TestDllFile));
        }

    }
}
