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

        private string ExtensionDirectory;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
        private const string zipPrefix = "microsoft.codecoverage";
        private const string zipDirectoryName = "msCodeCoverage";
        private const string fccSettingsTemplate = "fineCodeCoverageSettings.xml";

        [ImportingConstructor]
        public MsCodeCoverageRunSettingsService(IToolFolder toolFolder, IToolZipProvider toolZipProvider)
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
        }

        public void Initialize(string appDataFolder, CancellationToken cancellationToken)
        {
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix), cancellationToken);
            MsCodeCoveragePath = Path.Combine(zipDestination, "build", "netstandard1.0");
            ShimPath = Path.Combine(zipDestination, "build", "netstandard1.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private void PrepareOutputFolder(string outputFolder)
        {
            string destination = Path.Combine(outputFolder, Path.GetFileName(ShimPath));
            if (!File.Exists(destination))
            {
                File.Copy(ShimPath, destination);
            }
        }

        private string testResultsDirectory;

        public void PrepareRunSettings(string solutionPath, ITestOperation testOperation)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                List<ICoverageProject> coverageProjects = await testOperation.GetCoverageProjectsAsync();
                var excluded = new HashSet<string>();

                foreach (var p in coverageProjects)
                {
                    // TODO only call this if the project is a .net framework project
                    // Not needed for .net core
                    PrepareOutputFolder(p.ProjectOutputFolder);
                }

                foreach (var p in coverageProjects.Where(x => !x.Settings.IncludeTestAssembly))
                {
                    excluded.Add(Path.GetFileName(p.TestDllFile));
                }

                string excludeXml = "";
                foreach (var ex in excluded)
                {
                    excludeXml += $"<ModulePath>{ex}</ModulePath>";
                }

                testResultsDirectory = Path.Combine(solutionPath, ".fcc", "TestResults");
                if (Directory.Exists(testResultsDirectory))
                {
                    Directory.Delete(testResultsDirectory, true);
                }
                Directory.CreateDirectory(testResultsDirectory);

                var fccTemplate = Path.Combine(solutionPath, fccSettingsTemplate);
                string runSettings = "";
                if (!File.Exists(fccTemplate))
                {
                    File.Copy(Path.Combine(ExtensionDirectory, fccSettingsTemplate), fccTemplate);
                }
                runSettings = File.ReadAllText(fccTemplate);

                var runsettingsFile = Path.Combine(solutionPath, ".fcc", "fcc.runsettings");
                var preparedRunsettings = runSettings.Replace("%resultsDir%", testResultsDirectory)
                      .Replace("%testAdapter%", MsCodeCoveragePath)
                      .Replace("%exclude%", excludeXml);
                File.WriteAllText(runsettingsFile, preparedRunsettings);
                testOperation.SetRunSettings(runsettingsFile);
            }
             );
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
