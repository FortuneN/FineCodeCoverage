using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Xml.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;
using System.Threading;

namespace FineCodeCoverage.Engine.Model
{
    internal class CoverageProject : ICoverageProject
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ICoverageProjectSettingsManager settingsManager;
        private readonly IReferencedProjectsHelper referencedProjectsHelper;
        private XElement projectFileXElement;
        private IAppOptions settings;
        private string targetFramework;
        private readonly string fccFolderName = "fine-code-coverage";
        private readonly string buildOutputFolderName = "build-output";
        private string buildOutputPath;
        private bool? isDotNetSdkStyle;
        private string BuildOutputPath
        {
            get
            {
                if(buildOutputPath == null)
                {
                    var adjacentBuildOutput = appOptionsProvider.Get().AdjacentBuildOutput;
                    if (adjacentBuildOutput)
                    {
                        // Net framework - Debug | Debug-NET45
                        // SDK style - Debug/netcoreapp3.1 etc
                        var projectOutputDirectory = new DirectoryInfo(ProjectOutputFolder);
                        var projectOutputDirectoryName = projectOutputDirectory.Name;
                        var containingDirectoryPath = projectOutputDirectory.Parent.FullName;
                        buildOutputPath = Path.Combine(containingDirectoryPath, $"{fccFolderName}-{projectOutputDirectoryName}");
                    }
                    else
                    {
                        buildOutputPath = Path.Combine(FCCOutputFolder, buildOutputFolderName);
                    }
                }
                return buildOutputPath;
                
            }
        }
        private readonly string coverageToolOutputFolderName = "coverage-tool-output";

        public CoverageProject(
            IAppOptionsProvider appOptionsProvider, 
            IFileSynchronizationUtil fileSynchronizationUtil, 
            ICoverageProjectSettingsManager settingsManager,
            IReferencedProjectsHelper referencedProjectsHelper)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.settingsManager = settingsManager;
            this.referencedProjectsHelper = referencedProjectsHelper;
        }

        public string FCCOutputFolder => Path.Combine(ProjectOutputFolder, fccFolderName);
        public bool IsDotNetSdkStyle()
        {
            if (isDotNetSdkStyle.HasValue)
            {
                return isDotNetSdkStyle.Value;
            }

            isDotNetSdkStyle = ProjectFileXElement
            .DescendantsAndSelf()
            .Where(x =>
            {
                //https://docs.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk?view=vs-2019
                return IsRootProjectElementWithSdkAttribute(x) ||
                    IsRootProjectElementSdkElementChild(x) ||
                    IsRootImportElementWithSdkAttribute(x);
            })
            .Any();

            return isDotNetSdkStyle.Value;

            bool HasSdkAttribute(XElement x)
            {
                return x?.Attributes()?.FirstOrDefault(attr => attr?.Name?.LocalName?.Equals("Sdk", StringComparison.OrdinalIgnoreCase) == true) != null;
            }

            bool IsRootProjectElementWithSdkAttribute(XElement x)
            {
                return x?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
                    x?.Parent == null && HasSdkAttribute(x);
            }

            bool IsRootProjectElementSdkElementChild(XElement x)
            {
                return x?.Name?.LocalName?.Equals("Sdk", StringComparison.OrdinalIgnoreCase) == true &&
                    x?.Parent?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
                    x?.Parent?.Parent == null;
            }

            bool IsRootImportElementWithSdkAttribute(XElement x)
            {
                return x?.Name?.LocalName?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true &&
                    x?.Parent?.Name?.LocalName?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true &&
                    x?.Parent?.Parent == null && HasSdkAttribute(x);
            }
        }

        public string TestDllFile { get; set; }
        public string ProjectOutputFolder => Path.GetDirectoryName(TestDllFile);
        public string FailureDescription { get; set; }
        public string FailureStage { get; set; }
        public bool HasFailed => !string.IsNullOrWhiteSpace(FailureStage) || !string.IsNullOrWhiteSpace(FailureDescription);
        public string ProjectFile { get; set; }
        public Guid Id { get; set; }
        public string ProjectName { get; set; }
        public string CoverageOutputFile => Path.Combine(CoverageOutputFolder, $"{ProjectName}.coverage.xml");

        public IAppOptions Settings
        {
            get
            {
                if (settings == null)
                {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        settings = await settingsManager.GetSettingsAsync(this);
                    });
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
                }
                return settings;
            }
        }
        public string CoverageOutputFolder { get; set; }
        public string DefaultCoverageOutputFolder => Path.Combine(FCCOutputFolder, coverageToolOutputFolderName);

        public XElement ProjectFileXElement
        {
            get
            {
                if (projectFileXElement == null)
                {
                    projectFileXElement = LinqToXmlUtil.Load(ProjectFile, true);
                }
                return projectFileXElement;

            }
        }
        public List<IReferencedProject> ExcludedReferencedProjects { get; } = new List<IReferencedProject>();
        public List<IReferencedProject> IncludedReferencedProjects { get; set; } = new List<IReferencedProject>();
        public bool Is64Bit { get; set; }
        public string RunSettingsFile { get; set; }
        public bool IsDotNetFramework { get; private set; }
        public string TargetFramework {
            get => targetFramework;
            set
            {
                targetFramework = value;
                switch (targetFramework) {
                    case "Framework35":
                    case "Framework40":
                    case "Framework45":
                        IsDotNetFramework = true;
                        break;
                    case "FrameworkCore10":
                    case "FrameworkUap10":
                    case "None":
                        break;
                }

            }
        }

        public async Task StepAsync(string stepName, Func<ICoverageProject, Task> action)
        {
            if (HasFailed)
            {
                return;
            }

            try
            {
                await action(this);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                FailureStage = stepName;
                FailureDescription = exception.ToString();
            }
        }

        public async Task<CoverageProjectFileSynchronizationDetails> PrepareForCoverageAsync(CancellationToken cancellationToken,bool synchronizeBuildOuput = true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureDirectories();

            cancellationToken.ThrowIfCancellationRequested();
            CleanFCCDirectory();

            CoverageProjectFileSynchronizationDetails synchronizationDetails = null;
            if (synchronizeBuildOuput)
            {
                cancellationToken.ThrowIfCancellationRequested();
                synchronizationDetails = SynchronizeBuildOutput();
            }

            cancellationToken.ThrowIfCancellationRequested();
            await SetIncludedExcludedReferencedProjectsAsync();

            return synchronizationDetails;
        }

        private async Task SetIncludedExcludedReferencedProjectsAsync()
        {
            var referencedProjects = await referencedProjectsHelper.GetReferencedProjectsAsync(ProjectFile,() => ProjectFileXElement);
            SetExcludedReferencedProjects(referencedProjects);
            SetIncludedReferencedProjects(referencedProjects);
        }

        private void SetIncludedReferencedProjects(List<IExcludableReferencedProject> referencedProjects)
        {
            if (Settings.IncludeReferencedProjects)
            {
                IncludedReferencedProjects = new List<IReferencedProject>(referencedProjects);
            }
        }
        
        private void SetExcludedReferencedProjects(List<IExcludableReferencedProject> referencedProjects)
        {
            foreach (var referencedProject in referencedProjects)
            {
                if (referencedProject.ExcludeFromCodeCoverage)
                {
                    ExcludedReferencedProjects.Add(referencedProject);
                }
            }
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
        
        private void CleanFCCDirectory()
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
        
        private CoverageProjectFileSynchronizationDetails SynchronizeBuildOutput()
        {
            var start = DateTime.Now;
            var logs = fileSynchronizationUtil.Synchronize(ProjectOutputFolder, BuildOutputPath, fccFolderName);
            var duration = DateTime.Now - start;
            TestDllFile = Path.Combine(BuildOutputPath, Path.GetFileName(TestDllFile));
            return new CoverageProjectFileSynchronizationDetails
            {
                Logs = logs,
                Duration = duration
            };
        }

    }
}
