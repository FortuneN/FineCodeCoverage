using System.IO;
using AutoMoq;
using FineCodeCoverage.Engine.Coverlet;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class CoverletCollectorFolder_Tests
    {
        private AutoMoqer mocker;
        private CoverletCollectorFolder coverletCollectorFolder;
        private DirectoryInfo appDataFolder;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverletCollectorFolder = mocker.Create<CoverletCollectorFolder>();
            appDataFolder = CreateTemporaryDirectory();
        }

        [TearDown]
        public void DeleteCoverageOutputFolder()
        {
            appDataFolder.Delete(true);
        }

        public DirectoryInfo CreateTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
        }


        [Test]
        public void Should_Create_Zip_Destination_From_Version_And_Extract_If_Destination_Does_Not_Exist()
        {
            var zipDestination = coverletCollectorFolder.EnsureUnzipped(appDataFolder.FullName, new ZipDetails { Version = "3.0", Path = "zipPath" });
            mocker.Verify<IZipFile>(z => z.ExtractToDirectory("zipPath", zipDestination));
            Assert.IsTrue(Directory.Exists(zipDestination));
            var zipDestinationName = new DirectoryInfo(zipDestination).Name;
            Assert.AreEqual(zipDestinationName, "3.0");
        }

        [Test]
        public void Should_Delete_Old_Versions_When_Update_Zip()
        {
            var oldZipDestination = coverletCollectorFolder.EnsureUnzipped(appDataFolder.FullName, new ZipDetails { Version = "3.0", Path = "zipPath" });
            var newZipDestination = coverletCollectorFolder.EnsureUnzipped(appDataFolder.FullName, new ZipDetails { Version = "3.1", Path = "zipPath" });
            Assert.IsFalse(Directory.Exists(oldZipDestination));
            Assert.IsTrue(Directory.Exists(newZipDestination));
        }

        [Test]
        public void Should_Do_Nothing_If_Version_Has_Not_Changed()
        {
            var zipDestination = coverletCollectorFolder.EnsureUnzipped(appDataFolder.FullName, new ZipDetails { Version = "3.0", Path = "zipPath" });
            coverletCollectorFolder.EnsureUnzipped(appDataFolder.FullName, new ZipDetails { Version = "3.0", Path = "zipPath" });
            mocker.Verify<IZipFile>(z => z.ExtractToDirectory("zipPath", zipDestination),Times.Once());
            Assert.IsTrue(Directory.Exists(zipDestination));
        }
        
    }
}