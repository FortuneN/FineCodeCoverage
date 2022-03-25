using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(ITemplatedRunSettingsService))]
    internal class TemplatedRunSettingsService : ITemplatedRunSettingsService
    {
        private readonly IRunSettingsTemplate runSettingsTemplate;
        private readonly ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider;
        private readonly IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory;
        private readonly IProjectRunSettingsGenerator projectRunSettingsGenerator;

        private class TemplatedCoverageProjectRunSettingsResult : ICoverageProjectRunSettings
        {
            public ICoverageProject CoverageProject { get; set; }
            public string RunSettings { get; set; }
            public string CustomTemplatePath { get; internal set; }
            public bool ReplacedTestAdapter { get; internal set; }
        }

        private class ProjectRunSettingsFromTemplateResult : IProjectRunSettingsFromTemplateResult
        {
            private class ExceptionReasonImpl : IExceptionReason
            {
                public ExceptionReasonImpl(Exception exc, string reason)
                {
                    Exception = exc;
                    Reason = reason;
                }

                public Exception Exception { get; }

                public string Reason { get; }
            }
            public IExceptionReason ExceptionReason { get; set; }

            public List<string> CustomTemplatePaths { get; set; }

            public List<ICoverageProject> CoverageProjectsWithFCCMsTestAdapter { get; set; }

            public static ProjectRunSettingsFromTemplateResult FromException(Exception exception, string reason)
            {
                return new ProjectRunSettingsFromTemplateResult
                {
                    ExceptionReason = new ExceptionReasonImpl(exception, reason)
                };
            }
        }

        [ImportingConstructor]
        public TemplatedRunSettingsService(
            IRunSettingsTemplate runSettingsTemplate,
            ICustomRunSettingsTemplateProvider customRunSettingsTemplateProvider,
            IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory,
            IProjectRunSettingsGenerator projectRunSettingsGenerator
        )
        {
            this.runSettingsTemplate = runSettingsTemplate;
            this.customRunSettingsTemplateProvider = customRunSettingsTemplateProvider;
            this.runSettingsTemplateReplacementsFactory = runSettingsTemplateReplacementsFactory;
            this.projectRunSettingsGenerator = projectRunSettingsGenerator;
        }

        public async Task<IProjectRunSettingsFromTemplateResult> GenerateAsync(IEnumerable<ICoverageProject> coverageProjectsWithoutRunSettings, string solutionDirectory, string fccMsTestAdapterPath)
        {
            IEnumerable<TemplatedCoverageProjectRunSettingsResult> projectsRunSettings;
            try
            {
                projectsRunSettings = CreateProjectsRunSettings(
                    coverageProjectsWithoutRunSettings, solutionDirectory, fccMsTestAdapterPath);
            }
            catch (Exception exc)
            {
                return ProjectRunSettingsFromTemplateResult.FromException(exc, "Exception generating runsettings from template");
            }

            try
            {
                await projectRunSettingsGenerator.WriteProjectsRunSettingsAsync(projectsRunSettings);
            }
            catch (Exception exc)
            {
                await Tryer.TryAsync(
                    () => projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(coverageProjectsWithoutRunSettings)
                );

                return ProjectRunSettingsFromTemplateResult.FromException(exc, "Exception writing templated runsettings");
            }

            return CreateSuccessResult(projectsRunSettings);
        }

        private IProjectRunSettingsFromTemplateResult CreateSuccessResult(IEnumerable<TemplatedCoverageProjectRunSettingsResult> templatedCoverageProjectsRunSettingsResult)
        {
            List<string> customTemplatePaths = new List<string>();
            List<ICoverageProject> coverageProjectsWithFCCMsTestAdapter = new List<ICoverageProject>();
            foreach (var templatedCoverageProjectRunSettingsResult in templatedCoverageProjectsRunSettingsResult)
            {
                if (templatedCoverageProjectRunSettingsResult.ReplacedTestAdapter)
                {
                    coverageProjectsWithFCCMsTestAdapter.Add(templatedCoverageProjectRunSettingsResult.CoverageProject);
                }
                if (templatedCoverageProjectRunSettingsResult.CustomTemplatePath != null)
                {
                    customTemplatePaths.Add(templatedCoverageProjectRunSettingsResult.CustomTemplatePath);
                }
            }

            return new ProjectRunSettingsFromTemplateResult
            {
                CustomTemplatePaths = customTemplatePaths,
                CoverageProjectsWithFCCMsTestAdapter = coverageProjectsWithFCCMsTestAdapter
            };
        }

        private List<TemplatedCoverageProjectRunSettingsResult> CreateProjectsRunSettings(
            IEnumerable<ICoverageProject> coverageProjects, 
            string solutionDirectory, 
            string fccMsTestAdapterPath
        )
        {
            return coverageProjects.Select(coverageProject =>
            {
                var projectDirectory = Path.GetDirectoryName(coverageProject.ProjectFile);
                var (runSettingsTemplate, customTemplatePath) = GetRunSettingsTemplate(projectDirectory, solutionDirectory);
                var templateReplaceResult = ReplaceTemplate(coverageProject, runSettingsTemplate, fccMsTestAdapterPath);

                return new TemplatedCoverageProjectRunSettingsResult
                {
                    CoverageProject = coverageProject,
                    RunSettings = templateReplaceResult.Replaced,
                    CustomTemplatePath = customTemplatePath,
                    ReplacedTestAdapter = templateReplaceResult.ReplacedTestAdapter
                };

            }).ToList();
        }

        private (string Template, string CustomPath) GetRunSettingsTemplate(string projectDirectory, string solutionDirectory)
        {
            string customPath = null;
            string template;
            var customRunSettingsTemplateDetails = customRunSettingsTemplateProvider.Provide(projectDirectory, solutionDirectory);
            if (customRunSettingsTemplateDetails != null)
            {
                customPath = customRunSettingsTemplateDetails.Path;
                template = runSettingsTemplate.ConfigureCustom(customRunSettingsTemplateDetails.Template);
            }
            else
            {
                template = runSettingsTemplate.ToString();
            }
            return (template, customPath);
        }

        private ITemplateReplacementResult ReplaceTemplate(ICoverageProject coverageProject, string runSettingsTemplate, string fccMsTestAdapterPath)
        {
            var replacements = runSettingsTemplateReplacementsFactory.Create(coverageProject, fccMsTestAdapterPath);

            return this.runSettingsTemplate.ReplaceTemplate(runSettingsTemplate, replacements, coverageProject.IsDotNetFramework);
        }

        public Task CleanUpAsync(List<ICoverageProject> coverageProjects)
        {
            return projectRunSettingsGenerator.RemoveGeneratedProjectSettingsAsync(coverageProjects);
        }
    }

}
