using NUnit.Framework;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System.IO;
using System;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using Moq;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    public class ShimCopier_Tests
    {
        private AutoMoqer autoMocker;
        private ShimCopier shimCopier;

        [SetUp]
        public void SetupSut()
        {
            autoMocker = new AutoMoqer();
            shimCopier = autoMocker.Create<ShimCopier>();
        }

        [Test]
        public void Should_Copy_Shim_For_Net_Framework_Projects_Where_Does_Not_Exist_In_Project_Output_Folder()
        {
            IEnumerable<ICoverageProject> coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject("NetFramework",true),
                CreateCoverageProject("",false),
            };
            
            var shimDestination = Path.Combine("NetFramework", Path.GetFileName("ShimPath"));
            var mockFileUtil = autoMocker.GetMock<IFileUtil>();

            mockFileUtil.Setup(file => file.Exists(shimDestination)).Returns(false);
            mockFileUtil.Setup(file => file.Copy("ShimPath", shimDestination));
            shimCopier.Copy("ShimPath", coverageProjects);

            mockFileUtil.VerifyAll();
            mockFileUtil.VerifyNoOtherCalls();
        }

        [Test]
        public void Should_Not_Copy_Shim_For_Net_Framework_Projects_If_Already_Exists()
        {
            IEnumerable<ICoverageProject> coverageProjects = new List<ICoverageProject>
            {
                CreateCoverageProject("NetFramework",true),
                CreateCoverageProject("",false),
            };

            var shimDestination = Path.Combine("NetFramework", Path.GetFileName("ShimPath"));
            var mockFileUtil = autoMocker.GetMock<IFileUtil>();

            mockFileUtil.Setup(file => file.Exists(shimDestination)).Returns(true);
            
            shimCopier.Copy("ShimPath", coverageProjects);

            mockFileUtil.Verify(file => file.Copy("ShimPath", shimDestination), Times.Never());
        }

        private ICoverageProject CreateCoverageProject(string projectOutputFolder, bool isNetFramework)
        {
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.SetupGet(cp => cp.ProjectOutputFolder).Returns(projectOutputFolder);
            mockCoverageProject.SetupGet(cp => cp.IsDotNetFramework).Returns(isNetFramework);
            return mockCoverageProject.Object;
        }
    }
}
