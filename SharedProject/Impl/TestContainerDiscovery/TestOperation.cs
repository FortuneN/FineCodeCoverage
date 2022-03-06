using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    internal class TestOperation : ITestOperation
    {
        private readonly Operation operation;
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IRunSettingsRetriever runSettingsRetriever;

        public TestOperation(Operation operation, ICoverageProjectFactory coverageProjectFactory, IRunSettingsRetriever runSettingsRetriever)
        {
            this.operation = operation;
            this.coverageProjectFactory = coverageProjectFactory;
            this.runSettingsRetriever = runSettingsRetriever;
        }
        public long FailedTests => operation.Response.FailedTests;

        public long TotalTests => operation.TotalTests;

        public Task<List<ICoverageProject>> GetCoverageProjectsAsync()
        {
            return GetCoverageProjectsAsync(operation.Configuration);
        }

        public void SetRunSettings(string filePath)
        {
            var userRunSettings = operation.Configuration.UserRunSettings;
            userRunSettings.GetType().GetMethod("SetActiveRunSettings", BindingFlags.Public | BindingFlags.Instance).Invoke(userRunSettings, new object[] { filePath });
        }

        private async System.Threading.Tasks.Task<List<ICoverageProject>> GetCoverageProjectsAsync(TestConfiguration testConfiguration)
        {
            var userRunSettings = testConfiguration.UserRunSettings;
            var testContainers = testConfiguration.Containers;
            List<ICoverageProject> coverageProjects = new List<ICoverageProject>();
            foreach (var container in testContainers)
            {
                var project = coverageProjectFactory.Create();
                coverageProjects.Add(project);
                project.ProjectName = container.ProjectName;
                project.TestDllFile = container.Source;
                project.Is64Bit = container.TargetPlatform.ToString().ToLower().Equals("x64");

                var containerData = container.ProjectData;
                project.ProjectFile = container.ProjectData.ProjectFilePath;
                project.RunSettingsFile = await runSettingsRetriever.GetRunSettingsFileAsync(userRunSettings, containerData);

            }
            return coverageProjects;
        }

    }
}



