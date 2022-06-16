using System.Collections.Generic;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    internal class TestOperation : ITestOperation
    {
        private readonly TestRunRequest testRunRequest;
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;

        public TestOperation(TestRunRequest testRunRequest, ICoverageProjectFactory coverageProjectFactory, IRunSettingsRetriever runSettingsRetriever)
        {
            this.testRunRequest = testRunRequest;
            this.coverageProjectFactory = coverageProjectFactory;
            this.runSettingsRetriever = runSettingsRetriever;
        }
        public long FailedTests => testRunRequest.Response.FailedTests;

        public long TotalTests => testRunRequest.TotalTests;

        public string SolutionDirectory => testRunRequest.Configuration.SolutionDirectory;

        public Task<List<ICoverageProject>> GetCoverageProjectsAsync()
        {
            return GetCoverageProjectsAsync(testRunRequest.Configuration);
        }

        private async Task<List<ICoverageProject>> GetCoverageProjectsAsync(TestConfiguration testConfiguration)
        {
            var userRunSettings = testConfiguration.UserRunSettings;
            var testContainers = testConfiguration.Containers;
            List<ICoverageProject> coverageProjects = new List<ICoverageProject>();
            foreach (var container in testContainers)
            {
                var project = await coverageProjectFactory.CreateAsync();
                coverageProjects.Add(project);
                project.ProjectName = container.ProjectName;
                project.TestDllFile = container.Source;
                project.Is64Bit = container.TargetPlatform.ToString().ToLower().Equals("x64");
                project.TargetFramework = container.TargetFramework.ToString();
                var containerData = container.ProjectData;
                project.ProjectFile = container.ProjectData.ProjectFilePath;
                project.Id = containerData.Id;
                project.RunSettingsFile = await runSettingsRetriever.GetRunSettingsFileAsync(userRunSettings, containerData);

            }
            return coverageProjects;
        }

    }
}



