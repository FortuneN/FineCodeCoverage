using System;
using System.Collections.Generic;
using System.IO;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    class CoverageToolOutputManager_Tests
    {
        private AutoMoqer mocker;
        private Mock<ICoverageProject> mockProject1;
        private Mock<ICoverageProject> mockProject2;
        private List<ICoverageProject> coverageProjects;
        private List<int> callOrder;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            mockProject1 = new Mock<ICoverageProject>();
            mockProject1.Setup(p => p.FCCOutputFolder).Returns("p1output");
            mockProject1.Setup(p => p.ProjectName).Returns("project1");
            mockProject1.SetupProperty(p => p.CoverageOutputFolder);
            mockProject2 = new Mock<ICoverageProject>();
            mockProject2.Setup(p => p.FCCOutputFolder).Returns("p2output");
            mockProject2.Setup(p => p.ProjectName).Returns("project2");
            coverageProjects = new List<ICoverageProject> { mockProject1.Object, mockProject2.Object };
        }
        
        private void SetUpProviders(bool provider1First,string provider1Provides, string provider2Provides)
        {
            callOrder = new List<int>();
            var mockOrderMetadata1 = new Mock<IOrderMetadata>();
            mockOrderMetadata1.Setup(o => o.Order).Returns(provider1First? 1 : 2);
            var mockOrderMetadata2 = new Mock<IOrderMetadata>();
            mockOrderMetadata2.Setup(o => o.Order).Returns(provider1First ? 2 : 1);

            var mockCoverageToolOutputFolderProvider1 = new Mock<ICoverageToolOutputFolderProvider>();
            mockCoverageToolOutputFolderProvider1.Setup(p => p.Provide(coverageProjects)).Returns(provider1Provides).Callback(() => callOrder.Add(1));
            var mockCoverageToolOutputFolderProvider2 = new Mock<ICoverageToolOutputFolderProvider>();
            mockCoverageToolOutputFolderProvider2.Setup(p => p.Provide(coverageProjects)).Returns(provider2Provides).Callback(() => callOrder.Add(2));
            List<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>> lazyOrderedProviders = new List<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>>
            {
                new Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>(()=>mockCoverageToolOutputFolderProvider1.Object,mockOrderMetadata1.Object),
                new Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>(()=>mockCoverageToolOutputFolderProvider2.Object,mockOrderMetadata2.Object)
            };
            mocker.SetInstance<IEnumerable<Lazy<ICoverageToolOutputFolderProvider, IOrderMetadata>>>(lazyOrderedProviders);
        }

        [TestCase(true,1, 2)]
        [TestCase(false, 2, 1)]
        public void Should_Use_Providers_In_Order_When_Determining_CoverageProject_Output_Folder(bool provider1First, int expectedFirst, int expectedSecond)
        {
            SetUpProviders(provider1First, null, null);
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            Assert.AreEqual(callOrder, new List<int> { expectedFirst, expectedSecond });
        }

        [Test]
        public void Should_Stop_Asking_Providers_When_One_Provides_Value()
        {
            SetUpProviders(true, "_", "_");
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            Assert.AreEqual(callOrder, new List<int> { 1 });
        }

        [Test]
        public void Should_Try_Empty_Provided_Output_Folder()
        {
            SetUpProviders(true, "Provided", "_");
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            mocker.Verify<IFileUtil>(f => f.TryEmptyDirectory("Provided"));
        }


        [Test]
        public void Should_Log_When_Provided()
        {
            SetUpProviders(true, "Provided", "_");
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);
            mocker.Verify<ILogger>(l => l.Log("FCC output in Provided"));
        }

        [Test]
        public void Should_Set_CoverageOutputFolder_To_ProjectName_Sub_Folder_Of_Provided()
        {
            SetUpProviders(true, "Provided", "_");
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);

            var expectedProject1OutputFolder = Path.Combine("Provided", mockProject1.Object.ProjectName);
            var expectedProject2OutputFolder = Path.Combine("Provided", mockProject2.Object.ProjectName);
            mockProject1.VerifySet(p => p.CoverageOutputFolder = expectedProject1OutputFolder);
            mockProject2.VerifySet(p => p.CoverageOutputFolder = expectedProject2OutputFolder);

        }

        [Test]
        public void Should_Set_CoverageOutputFolder_To_Sub_Folder_Of_CoverageProject_FCCOutputFolder_For_All_When_Not_Provided()
        {
            SetUpProviders(true, null, null);
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);

            var expectedProject1OutputFolder = Path.Combine(mockProject1.Object.FCCOutputFolder, "coverage-tool-output");
            var expectedProject2OutputFolder = Path.Combine(mockProject2.Object.FCCOutputFolder, "coverage-tool-output");
            mockProject1.VerifySet(p => p.CoverageOutputFolder = expectedProject1OutputFolder);
            mockProject2.VerifySet(p => p.CoverageOutputFolder = expectedProject2OutputFolder);
        }
    
        [Test]
        public void Should_Output_Reports_To_First_Project_CoverageOutputFolder_When_Not_Provided()
        {
            SetUpProviders(true, null, null);
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);

            coverageToolOutputManager.OutputReports("unified html", "processed report", "unified xml");

            var firstProjectOutputFolder = mockProject1.Object.CoverageOutputFolder;

            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine(firstProjectOutputFolder, "index.html"), "unified html"));
            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine(firstProjectOutputFolder, "index-processed.html"), "processed report"));
            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine(firstProjectOutputFolder, "Cobertura.xml"), "unified xml"));
            
        }

        [Test]
        public void Should_Output_Reports_To_Provided_When_Not_Provided()
        {
            SetUpProviders(true, "Provided", null);
            var coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
            coverageToolOutputManager.SetProjectCoverageOutputFolder(coverageProjects);

            coverageToolOutputManager.OutputReports("unified html", "processed report", "unified xml");

            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine("Provided", "index.html"), "unified html"));
            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine("Provided", "index-processed.html"), "processed report"));
            mocker.Verify<IFileUtil>(f => f.WriteAllText(Path.Combine("Provided", "Cobertura.xml"), "unified xml"));

        }

    }
}
