using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverageTests.Test_helpers;
using NUnit.Framework;

namespace FineCodeCoverageTests.CoverageToolOutput_Tests
{
    class FccOutputExistenceCoverageToolOutputFolderSolutionProvider_Tests
    {
        private AutoMoqer mocker;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();

        }

        [Test]
        public void Should_Return_Null_If_No_Solution_Folder_Provided_To_It()
        {
            var provider = mocker.Create<FccOutputExistenceCoverageToolOutputFolderSolutionProvider>();
            Assert.Null(provider.Provide(() => null));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_Path_To_FCC_Output_Folder_In_Solution_Folder_If_Exists(bool exists)
        {
            var solutionFolder = "SolutionFolder";
            var expected = Path.Combine("SolutionFolder", "fcc-output");

            var mockFileUtil = mocker.GetMock<IFileUtil>();
            mockFileUtil.Setup(fu => fu.DirectoryExists(expected)).Returns(exists);

            var provider = mocker.Create<FccOutputExistenceCoverageToolOutputFolderSolutionProvider>();
            Assert.AreEqual(provider.Provide(() => solutionFolder), exists ? expected : null);
        }

        [Test]
        public void Should_Have_Second_Order()
        {
            MefOrderAssertions.TypeHasExpectedOrder(typeof(FccOutputExistenceCoverageToolOutputFolderSolutionProvider), 2);
        }
    }
}
