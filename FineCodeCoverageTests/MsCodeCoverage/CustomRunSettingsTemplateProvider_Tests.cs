using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System.IO;
using System;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class CustomRunSettingsTemplateProvider_Tests
    {
        private AutoMoqer autoMocker;
        private CustomRunSettingsTemplateProvider customRunSettingsTemplateProvider;
        private const string templateName = "fcc-ms-runsettings-template.xml";
        private const string projectDirectory = "ProjectDirectory";
        private const string solutionDirectory = "SolutionDirectory";
        private readonly string templateInProjectDirectory = Path.Combine(projectDirectory, templateName);
        private readonly string templateInSolutionDirectory = Path.Combine(solutionDirectory, templateName);


        [Flags]
        internal enum TemplateIn { None, ProjectDir, SolutionDir}

        [SetUp]
        public void Setup()
        {
            autoMocker = new AutoMoqer();
            customRunSettingsTemplateProvider = autoMocker.Create<CustomRunSettingsTemplateProvider>();
        }

        [TestCase(TemplateIn.ProjectDir)]
        [TestCase(TemplateIn.ProjectDir | TemplateIn.SolutionDir)]
        public void Should_Return_Non_Null_If_Found_In_The_Project_Directory(TemplateIn templateIn)
        {
            SetupFileUtil(templateIn);

            var results = customRunSettingsTemplateProvider.Provide(projectDirectory, solutionDirectory);
            Assert.AreEqual("ProjectRunSettings", results.Template);
            Assert.AreEqual(templateInProjectDirectory, results.Path);
        }

        [Test]
        public void Should_Return_Non_Null_If_Found_In_The_Solution_Directory()
        {
            SetupFileUtil(TemplateIn.SolutionDir);

            var results = customRunSettingsTemplateProvider.Provide(projectDirectory, solutionDirectory);
            Assert.AreEqual("SolutionRunSettings", results.Template);
            Assert.AreEqual(templateInSolutionDirectory, results.Path);
        }

        [Test]
        public void Should_Return_Null_If_Not_Found()
        {
            SetupFileUtil(TemplateIn.None);

            var results = customRunSettingsTemplateProvider.Provide(projectDirectory, solutionDirectory);
            Assert.Null(results);
        }

        [Test]
        public void Should_Not_Throw_For_Null_Directory()
        {
            SetupFileUtil(TemplateIn.ProjectDir);

            var results = customRunSettingsTemplateProvider.Provide(null, null);
            Assert.Null(results);
        }

        private void SetupFileUtil(TemplateIn templateIn)
        {
            var mockFileUtil = autoMocker.GetMock<IFileUtil>();
            mockFileUtil.Setup(f => f.Exists(templateInProjectDirectory)).Returns(templateIn.HasFlag(TemplateIn.ProjectDir));
            mockFileUtil.Setup(f => f.Exists(templateInSolutionDirectory)).Returns(templateIn.HasFlag(TemplateIn.SolutionDir));
            mockFileUtil.Setup(f => f.ReadAllText(templateInProjectDirectory)).Returns("ProjectRunSettings");
            mockFileUtil.Setup(f => f.ReadAllText(templateInSolutionDirectory)).Returns("SolutionRunSettings");
        }
    }
}
