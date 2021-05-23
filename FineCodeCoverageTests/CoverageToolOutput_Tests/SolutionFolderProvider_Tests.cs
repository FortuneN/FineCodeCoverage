using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using NUnit.Framework;

namespace FineCodeCoverageTests.CoverageToolOutput_Tests
{
    class SolutionFolderProvider_Tests
    {
        private string tempDirectory;
        private FileUtil fileUtil = new FileUtil();

        [SetUp]
        public void Create_Temp_Directory()
        {
            tempDirectory = fileUtil.CreateTempDirectory();
            File.WriteAllText(Path.Combine(tempDirectory, "my.sln"), "");
        }

        [TearDown]
        public void Delete_Temp_Directories()
        {
            fileUtil.TryDeleteDirectory(tempDirectory);
        }

        [Test]
        public void Should_Work_When_Solution_And_Test_Project_Are_In_Same_Folder()
        {
            var solutionFolderProvider = new SolutionFolderProvider();
            var provided = solutionFolderProvider.Provide(Path.Combine(tempDirectory, "my.proj"));

            Assert.AreEqual(tempDirectory, provided);
        }

        [Test]
        public void Should_Look_up_Directory_Tree()
        {
            var projectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory, "Project"));

            var solutionFolderProvider = new SolutionFolderProvider();
            var provided = solutionFolderProvider.Provide(Path.Combine(projectDirectory.FullName, "my.proj"));

            Assert.AreEqual(tempDirectory, provided);

        }

        [Test]
        public void Should_Return_Null_When_No_Solution_Directory_Ascendant()
        {
            tempDirectory = fileUtil.CreateTempDirectory();

            var solutionFolderProvider = new SolutionFolderProvider();
            var provided = solutionFolderProvider.Provide(Path.Combine(tempDirectory, "my.proj"));

            Assert.Null(provided);
        }
    }
}
