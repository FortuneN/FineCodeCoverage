using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.Test_helpers;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.CoverageToolOutput_Tests
{
    class CoverageToolOutputFolderFromSolutionProvider_Tests
    {
        private List<int> callOrder;
        private AutoMoqer mocker;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();

        }

        private void SetUpProviders(bool provider1First, string provider1Provides, string provider2Provides)
        {
            callOrder = new List<int>();
            var mockOrderMetadata1 = new Mock<IOrderMetadata>();
            mockOrderMetadata1.Setup(o => o.Order).Returns(provider1First ? 1 : 2);
            var mockOrderMetadata2 = new Mock<IOrderMetadata>();
            mockOrderMetadata2.Setup(o => o.Order).Returns(provider1First ? 2 : 1);

            var mockCoverageToolOutputFolderProvider1 = new Mock<ICoverageToolOutputFolderSolutionProvider>();
            mockCoverageToolOutputFolderProvider1.Setup(p => p.Provide(It.IsAny<Func<string>>())).Returns(provider1Provides).Callback(() => callOrder.Add(1));
            var mockCoverageToolOutputFolderProvider2 = new Mock<ICoverageToolOutputFolderSolutionProvider>();
            mockCoverageToolOutputFolderProvider2.Setup(p => p.Provide(It.IsAny<Func<string>>())).Returns(provider2Provides).Callback(() => callOrder.Add(2));
            List<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>> lazyOrderedProviders = new List<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>>
            {
                new Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>(()=>mockCoverageToolOutputFolderProvider1.Object,mockOrderMetadata1.Object),
                new Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>(()=>mockCoverageToolOutputFolderProvider2.Object,mockOrderMetadata2.Object)
            };
            mocker.SetInstance<IEnumerable<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>>>(lazyOrderedProviders);
        }

        public void Should_Have_First_Order()
        {
            MefOrderAssertions.TypeHasExpectedOrder(typeof(CoverageToolOutputFolderFromSolutionProvider), 1);
        }

        [TestCase(true, 1, 2)]
        [TestCase(false, 2, 1)]
        public void Should_Use_Providers_In_Order(bool provider1First, int expectedFirst, int expectedSecond)
        {
            SetUpProviders(provider1First, null, null);
            var coverageToolOutputFolderFromSolutionProvider = mocker.Create<CoverageToolOutputFolderFromSolutionProvider>();
            coverageToolOutputFolderFromSolutionProvider.Provide(null);
            Assert.AreEqual(callOrder, new List<int> { expectedFirst, expectedSecond });
        }
        //need to check if there is a coverage project ?
        [Test]
        public void Should_Stop_Asking_Providers_When_One_Returns_The_Folder()
        {
            SetUpProviders(true, "Folder", "_");
            var coverageToolOutputFolderFromSolutionProvider = mocker.Create<CoverageToolOutputFolderFromSolutionProvider>();
            Assert.AreEqual(coverageToolOutputFolderFromSolutionProvider.Provide(null), "Folder");
            Assert.AreEqual(callOrder, new List<int> { 1 });
        }

        [Test]
        public void Should_Provide_The_Solution_Folder_Once_From_The_Solution_Folder_Provider_Wth_ProjectFile_Of_First_CoverageProject()
        {
            var mockProject1 = new Mock<ICoverageProject>();
            mockProject1.Setup(p => p.ProjectFile).Returns("project.csproj");
            var mockProject2 = new Mock<ICoverageProject>();
            mockProject2.Setup(p => p.ProjectFile).Returns("project2.csproj");
            var coverageProjects = new List<ICoverageProject> { mockProject1.Object, mockProject2.Object };

            var mockOrderMetadata1 = new Mock<IOrderMetadata>();
            mockOrderMetadata1.Setup(o => o.Order).Returns(1);

            var mockCoverageToolOutputFolderProvider1 = new Mock<ICoverageToolOutputFolderSolutionProvider>();
            Func<string> solutionFolderProviderFunc = null;
            mockCoverageToolOutputFolderProvider1.Setup(p => p.Provide(It.IsAny<Func<string>>()))
                .Callback<Func<string>>(solnFolderProvider =>
                {
                    solutionFolderProviderFunc = solnFolderProvider;
                });
            List<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>> lazyOrderedProviders = new List<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>>
            {
                new Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>(()=>mockCoverageToolOutputFolderProvider1.Object,mockOrderMetadata1.Object),
            };
            mocker.SetInstance<IEnumerable<Lazy<ICoverageToolOutputFolderSolutionProvider, IOrderMetadata>>>(lazyOrderedProviders);

            var mockSolutionFolderProvider = mocker.GetMock<ISolutionFolderProvider>();
            mockSolutionFolderProvider.Setup(sfp => sfp.Provide("project.csproj")).Returns("SolutionPath");
            var coverageToolOutputFolderFromSolutionProvider = mocker.Create<CoverageToolOutputFolderFromSolutionProvider>();

            coverageToolOutputFolderFromSolutionProvider.Provide(coverageProjects);

            var solutionFolder = solutionFolderProviderFunc();
            var solutionFolder2 = solutionFolderProviderFunc();
            Assert.AreEqual(solutionFolder, "SolutionPath");
            Assert.AreEqual(solutionFolder2, "SolutionPath");
            mockSolutionFolderProvider.Verify(sfp => sfp.Provide("project.csproj"), Times.Once());

        }
    }
}
