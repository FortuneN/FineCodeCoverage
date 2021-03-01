using System.IO;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using NUnit.Framework;

namespace Test
{
    public class ToolZipProvider_Tests
    {
        private DirectoryInfo extensionDirectory;
        private DirectoryInfo zippedToolsDirectory;

        [SetUp]
        public void CreateCoverageOutputFolder()
        {
            extensionDirectory = CreateTemporaryDirectory();
            zippedToolsDirectory = extensionDirectory.CreateSubdirectory(ToolZipProvider.ZippedToolsDirectoryName);
        }

        [TearDown]
        public void DeleteCoverageOutputFolder()
        {
            extensionDirectory.Delete(true);
        }
        public DirectoryInfo CreateTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);

        }

        [TestCase("1.0.0")]
        [TestCase("3.0.0")]
        public void Should_Provide_Zip_In_Extension_Directory_By_Naming_Convention(string version)
        {
            var zipPrefix = "zipPrefix";
            var zipPath = Path.Combine(zippedToolsDirectory.FullName, $"{zipPrefix}.{version}.zip");
            File.WriteAllText(zipPath, "");
            var mocker = new AutoMoqer();
            var zipProvider = mocker.Create<ToolZipProvider>();
            zipProvider.ExtensionDirectory = extensionDirectory.FullName;
            var zipDetails = zipProvider.ProvideZip(zipPrefix);
            Assert.AreEqual(zipPath, zipDetails.Path);
            Assert.AreEqual(version, zipDetails.Version);
        }
    }
}