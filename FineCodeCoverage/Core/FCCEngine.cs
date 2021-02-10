using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Engine.Utilities;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(IFCCEngine))]
    internal class FCCEngine : IFCCEngine
    {
        private object colorThemeService;
        private string CurrentTheme => $"{((dynamic)colorThemeService)?.CurrentTheme?.Name}".Trim();
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        public event UpdateMarginTagsDelegate UpdateMarginTags;
        public event UpdateOutputWindowDelegate UpdateOutputWindow;

        private string HtmlFilePath;
        public string AppDataFolder { get; private set; }
        private CoverageReport coverageReport;
        private readonly ICoverletUtil coverletUtil;
        private readonly IOpenCoverUtil openCoverUtil;
        private readonly ICoberturaUtil coberturaUtil;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IReportGeneratorUtil reportGeneratorUtil;
        private readonly IProcessUtil processUtil;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;
        private readonly IAppDataFolder appDataFolder;

        public List<CoverageLine> CoverageLines { get; private set; } = new List<CoverageLine>();

        [ImportingConstructor]
        public FCCEngine(
            ICoverletUtil coverletUtil,
            IOpenCoverUtil openCoverUtil,
            ICoberturaUtil coberturaUtil,
            IMsTestPlatformUtil msTestPlatformUtil,
            IReportGeneratorUtil reportGeneratorUtil,
            IProcessUtil processUtil,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger,
            IAppDataFolder appDataFolder,
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
            this.coverletUtil = coverletUtil;
            this.openCoverUtil = openCoverUtil;
            this.coberturaUtil = coberturaUtil;
            this.msTestPlatformUtil = msTestPlatformUtil;
            this.reportGeneratorUtil = reportGeneratorUtil;
            this.processUtil = processUtil;
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.appDataFolder = appDataFolder;
            colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));

        }
        public void Initialize()
        {
            appDataFolder.Initialize();
            var appDataFolderPath = appDataFolder.DirectoryPath;
            
            reportGeneratorUtil.Initialize(appDataFolderPath);
            msTestPlatformUtil.Initialize(appDataFolderPath);
            openCoverUtil.Initialize(appDataFolderPath);

            coverletUtil.Initialize(appDataFolderPath);
        }

        
        public IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber)
        {
            return CoverageLines
            .AsParallel()
            .Where(x => x.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
            .ToArray();
        }

        public string[] GetSourceFiles(string assemblyName, string qualifiedClassName)
        {
            // Note : There may be more than one file; e.g. in the case of partial classes

            var package = coverageReport
                .Packages.Package
                .SingleOrDefault(x => x.Name.Equals(assemblyName));

            if (package == null)
            {
                return new string[0];
            }

            var classFiles = package
                .Classes.Class
                .Where(x => x.Name.Equals(qualifiedClassName))
                .Select(x => x.Filename)
                .ToArray();

            return classFiles;
        }

        public void ClearUI()
        {
            CoverageLines.Clear();
            UpdateMarginTags?.Invoke(null);
            UpdateOutputWindow?.Invoke(null);
        }

        public void StopCoverage()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }
        private void SetCancellationToken()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            processUtil.CancellationToken = cancellationToken;
        }
        private void Reset()
        {
            StopCoverage();
            SetCancellationToken();
            HtmlFilePath = null;
            CoverageLines.Clear();
        }

        private async System.Threading.Tasks.Task RunCoverToolAsync(CoverageProject project)
        {
            if (project.IsDotNetSdkStyle())
            {
                await coverletUtil.RunCoverletAsync(project, true);
            }
            else
            {
                await openCoverUtil.RunOpenCoverAsync(project, true);
            }
        }

        private async System.Threading.Tasks.Task PrepareCoverageProjectsAsync(List<CoverageProject> coverageProjects)
        {
            foreach (var project in coverageProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(project.ProjectFile))
                {
                    project.FailureDescription = $"Unsupported project type for DLL '{project.TestDllFile}'";
                    continue;
                }

                if (!project.Settings.Enabled)
                {
                    project.FailureDescription = $"Disabled";
                    continue;
                }

                await project.PrepareForCoverageAsync();
            }
        }

        public void ReloadCoverage(Func<System.Threading.Tasks.Task<List<CoverageProject>>> coverageRequestCallback)
        {
            Reset();

            var coverageTask = System.Threading.Tasks.Task.Run(async () =>
              {

                  logger.Log("================================== START ==================================");

                  var coverageProjects = await coverageRequestCallback();

                  // process pipeline

                  await PrepareCoverageProjectsAsync(coverageProjects);

                  foreach (var coverageProject in coverageProjects)
                  {
                      cancellationToken.ThrowIfCancellationRequested();
                      await coverageProject.StepAsync("Run Coverage Tool", RunCoverToolAsync);
                  }

                  var passedProjects = coverageProjects.Where(x => !x.HasFailed);

                  var coverOutputFiles = passedProjects
                          .Select(x => x.CoverageOutputFile)
                          .ToArray();

                  if (coverOutputFiles.Any())
                  {
                      cancellationToken.ThrowIfCancellationRequested();
                      // run reportGenerator process

                      var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

                      var result = await reportGeneratorUtil.RunReportGeneratorAsync(coverOutputFiles, darkMode, true);

                      if (result.Success)
                      {
                          // update CoverageLines

                          coverageReport = coberturaUtil.ProcessCoberturaXmlFile(result.UnifiedXmlFile, out var coverageLines);
                          CoverageLines = coverageLines;
                          // update HtmlFilePath

                          reportGeneratorUtil.ProcessUnifiedHtmlFile(result.UnifiedHtmlFile, darkMode, out var htmlFilePath);
                          HtmlFilePath = htmlFilePath;
                      }

                      // update margins

                      {
                          UpdateMarginTagsEventArgs updateMarginTagsEventArgs = null;

                          try
                          {
                              updateMarginTagsEventArgs = new UpdateMarginTagsEventArgs
                              {
                              };
                          }
                          catch
                          {
                              // ignore
                          }
                          finally
                          {
                              UpdateMarginTags?.Invoke(updateMarginTagsEventArgs);
                          }
                      }

                      // update output window

                      {
                          UpdateOutputWindowEventArgs updateOutputWindowEventArgs = null;

                          try
                          {
                              if (!string.IsNullOrEmpty(HtmlFilePath))
                              {
                                  updateOutputWindowEventArgs = new UpdateOutputWindowEventArgs
                                  {
                                      HtmlContent = File.ReadAllText(HtmlFilePath)
                                  };
                              }
                          }
                          catch
                          {
                              // ignore
                          }
                          finally
                          {
                              UpdateOutputWindow?.Invoke(updateOutputWindowEventArgs);
                          }
                      }

                  }


                  // log

                  logger.Log("================================== DONE ===================================");

                  cancellationTokenSource.Dispose();
                  cancellationTokenSource = null;
              }, cancellationToken).ContinueWith(t =>
              {
                  if (t.Status == System.Threading.Tasks.TaskStatus.Faulted)
                  {
                      logger.Log("Error processing unit test events", t.Exception);
                  }
              }, System.Threading.Tasks.TaskScheduler.Default);
            
        }

    }

}