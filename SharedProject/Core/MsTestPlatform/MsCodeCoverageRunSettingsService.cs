using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

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

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(IToolFolder toolFolder, IToolZipProvider toolZipProvider)
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
            var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var runSettingsPath = Path.Combine(extensionDirectory, fccSettingsTemplate);
            runSettings = File.ReadAllText(runSettingsPath);
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

        private void CreateCleanResultsDirectory(string solutionPath)
        {
            testResultsDirectory = Path.Combine(solutionPath, fccSolutionFolder, "TestResults");
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

        public void PrepareRunSettings(string solutionPath, ITestOperation testOperation)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var uiThread = ThreadHelper.CheckAccess();
                List<ICoverageProject> coverageProjects = await testOperation.GetCoverageProjectsAsync();

                CopyShimForNetFrameworkProjects(coverageProjects);
                CreateCleanResultsDirectory(solutionPath);

                var runsettingsFile = Path.Combine(solutionPath, fccSolutionFolder, "fcc.runsettings");
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
