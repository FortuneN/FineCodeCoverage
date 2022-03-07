using EnvDTE;
using EnvDTE80;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    [Export(typeof(IMsCodeCoverageRunSettingsService))]
    internal class MsCodeCoverageRunSettingsService : IMsCodeCoverageRunSettingsService
    {
        public string MsCodeCoveragePath { get; private set; }

        public string ShimPath { get; private set; }

        private readonly string runSettings;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
        private const string zipPrefix = "microsoft.codecoverage";
        private const string zipDirectoryName = "msCodeCoverage";
        private const string fccSettingsTemplate = "fineCodeCoverageSettings.xml";
        private const string fccSolutionFolder = ".fcc";
        private string testResultsDirectory;
        private string solutionDirectoryPath;
        private DTE2 dte;

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(
            IToolFolder toolFolder, 
            IToolZipProvider toolZipProvider, 
            ISolutionEvents solutionEvents,
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
            var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var runSettingsPath = Path.Combine(extensionDirectory, fccSettingsTemplate);
            runSettings = File.ReadAllText(runSettingsPath);
            solutionEvents.AfterOpen += SolutionEvents_AfterOpen;
            dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            Assumes.Present(dte);
        }

        private void SolutionEvents_AfterOpen(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await SetSolutionDirectoryPathAsync();
            });
        }

        private async Task SetSolutionDirectoryPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            solutionDirectoryPath =  Path.GetDirectoryName(dte.Solution.FileName);
        }

        public void Initialize(string appDataFolder, CancellationToken cancellationToken)
        {
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
            MsCodeCoveragePath = Path.Combine(zipDestination, "build", "netstandard1.0");
            ShimPath = Path.Combine(zipDestination, "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
        }

        private void CopyShim(string outputFolder)
        {
            string destination = Path.Combine(outputFolder, Path.GetFileName(ShimPath));
            if (!File.Exists(destination))
            {
                File.Copy(ShimPath, destination);
            }
        }

        private void CopyShimForNetFrameworkProjects(List<ICoverageProject> coverageProjects)
        {
            var netFrameworkCoverageProjects = coverageProjects.Where(cp => !cp.IsDotNetSdkStyle());
            foreach(var netFrameworkCoverageProject in netFrameworkCoverageProjects)
            {
                CopyShim(netFrameworkCoverageProject.ProjectOutputFolder);
            }
        }

        private void CreateCleanResultsDirectory()
        {
            testResultsDirectory = Path.Combine(solutionDirectoryPath, fccSolutionFolder, "TestResults");
            if (Directory.Exists(testResultsDirectory))
            {
                Directory.Delete(testResultsDirectory, true);
            }
            Directory.CreateDirectory(testResultsDirectory);
        }

        private string CreateRunSettings(List<ICoverageProject> coverageProjects)
        {
            var excluded = new HashSet<string>();
            foreach (var p in coverageProjects.Where(x => !x.Settings.IncludeTestAssembly))
            {
                excluded.Add(Path.GetFileName(p.TestDllFile));
            }

            string excludeXml = "";
            foreach (var ex in excluded)
            {
                excludeXml += $"<ModulePath>{ex}</ModulePath>";
            }

            return runSettings.Replace("%resultsDir%", testResultsDirectory)
                      .Replace("%testAdapter%", MsCodeCoveragePath)
                      .Replace("%exclude%", excludeXml);

        }

        private async Task EnsureSolutionDirectoryPathAsync()
        {
            if (solutionDirectoryPath == null)
            {
                await SetSolutionDirectoryPathAsync();
            }
        }

        public void PrepareRunSettings(ITestOperation testOperation)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await EnsureSolutionDirectoryPathAsync();
                List<ICoverageProject> coverageProjects = await testOperation.GetCoverageProjectsAsync();

                CopyShimForNetFrameworkProjects(coverageProjects);

                CreateCleanResultsDirectory();

                var runsettingsFile = Path.Combine(solutionDirectoryPath, fccSolutionFolder, "fcc.runsettings");
                File.WriteAllText(runsettingsFile, CreateRunSettings(coverageProjects));
                testOperation.SetRunSettings(runsettingsFile);
            });
        }

        public IList<string> GetCoverageFilesFromLastRun()
        {
            var outputFiles = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories(testResultsDirectory))
            {
                var coverage = Directory.EnumerateFiles(dir).FirstOrDefault(x => x.EndsWith(".xml"));
                if (coverage != null)
                {
                    outputFiles.Add(coverage);
                }
            }
            return outputFiles;
        }
    }
}
