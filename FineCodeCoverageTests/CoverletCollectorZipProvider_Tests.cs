using System.IO;
using AutoMoq;
using FineCodeCoverage.Engine.Coverlet;
using NUnit.Framework;

namespace Test
{
    public class CoverletCollectorZipProvider_Tests
    {
        private DirectoryInfo extensionDirectory;
        private DirectoryInfo coverletDirectory;

        [SetUp]
        public void CreateCoverageOutputFolder()
        {
            extensionDirectory = CreateTemporaryDirectory();
            var coreDirectory = extensionDirectory.CreateSubdirectory("Core");
            coverletDirectory = coreDirectory.CreateSubdirectory("Coverlet");
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
            var zipPath = Path.Combine(coverletDirectory.FullName, $"coverlet.collector.{version}.zip");
            File.WriteAllText(zipPath, "");
            var mocker = new AutoMoqer();
            var zipProvider = mocker.Create<CoverletCollectorZipProvider>();
            zipProvider.ExtensionDirectory = extensionDirectory.FullName;
            var zipDetails = zipProvider.ProvideZip();
            Assert.AreEqual(zipPath, zipDetails.Path);
            Assert.AreEqual(version, zipDetails.Version);
        }
    }
}