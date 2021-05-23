using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.Test_helpers;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.CoverageToolOutput_Tests
{
    class AppOptionsCoverageToolOutputFolderSolutionProvider_Tests
    {
        private AutoMoqer mocker;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();

        }

        [TestCase(null)]
        [TestCase("")]
        public void Should_Return_Null_Without_Getting_Solution_Folder_When_AppOption_FCCSolutionOutputDirectoryName_NotSet(string optionValue)
        {
            var mockAppOptionsProvider = mocker.GetMock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(options => options.FCCSolutionOutputDirectoryName).Returns(optionValue);
            mockAppOptionsProvider.Setup(aop => aop.Get()).Returns(mockAppOptions.Object);

            var provider = mocker.Create<AppOptionsCoverageToolOutputFolderSolutionProvider>();
            var providedSolutionFolder = false;
            Assert.Null(provider.Provide(() =>
            {
                providedSolutionFolder = true;
                return null;
            }));

            Assert.False(providedSolutionFolder);
        }

        [Test]
        public void Should_Return_Null_If_No_Solution_Folder_Provided_To_It()
        {
            var mockAppOptionsProvider = mocker.GetMock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(options => options.FCCSolutionOutputDirectoryName).Returns("Value");
            mockAppOptionsProvider.Setup(aop => aop.Get()).Returns(mockAppOptions.Object);
            var provider = mocker.Create<AppOptionsCoverageToolOutputFolderSolutionProvider>();
            Assert.Null(provider.Provide(() => null));
        }

        [Test]
        public void Should_Combine_The_Solution_Folder_With_FCCSolutionOutputDirectoryName()
        {
            var mockAppOptionsProvider = mocker.GetMock<IAppOptionsProvider>();
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(options => options.FCCSolutionOutputDirectoryName).Returns("FCCOutput");
            mockAppOptionsProvider.Setup(aop => aop.Get()).Returns(mockAppOptions.Object);
            var provider = mocker.Create<AppOptionsCoverageToolOutputFolderSolutionProvider>();
            Assert.AreEqual(provider.Provide(() => "SolutionFolder"), Path.Combine("SolutionFolder", "FCCOutput"));
        }

        [Test]
        public void Should_Have_First_Order()
        {
            MefOrderAssertions.TypeHasExpectedOrder(typeof(AppOptionsCoverageToolOutputFolderSolutionProvider), 1);
        }
    }
}
