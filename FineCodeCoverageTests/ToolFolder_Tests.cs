using System.IO;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class ToolFolder_Tests
    {
        private AutoMoqer mocker;
        private ToolFolder toolFolder;
        private DirectoryInfo appDataFolder;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            toolFolder = mocker.Create<ToolFolder>();
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
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder.FullName,"ToolFolder", new ZipDetails { Version = "3.0", Path = "zipPath" });
            mocker.Verify<IZipFile>(z => z.ExtractToDirectory("zipPath", zipDestination));
            Assert.IsTrue(Directory.Exists(zipDestination));
            var zipDestinationDirectory = new DirectoryInfo(zipDestination);
            Assert.AreEqual("ToolFolder", zipDestinationDirectory.Parent.Name);
            Assert.AreEqual(appDataFolder.FullName, zipDestinationDirectory.Parent.Parent.FullName);
            var zipDestinationName = new DirectoryInfo(zipDestination).Name;
            Assert.AreEqual(zipDestinationName, "3.0");
        }

        [Test]
        public void Should_Delete_Old_Versions_When_Update_Zip()
        {
            var oldZipDestination = toolFolder.EnsureUnzipped(appDataFolder.FullName,"ToolFolder", new ZipDetails { Version = "3.0", Path = "zipPath" });
            var toolDirectoryPath = new DirectoryInfo(oldZipDestination).Parent.FullName;
            var oldFileShouldBeDeleted = Path.Combine(toolDirectoryPath, "somefile.txt");
            File.WriteAllText(oldFileShouldBeDeleted, "");
            var newZipDestination = toolFolder.EnsureUnzipped(appDataFolder.FullName,"ToolFolder", new ZipDetails { Version = "3.1", Path = "zipPath" });
            Assert.IsFalse(Directory.Exists(oldZipDestination));
            Assert.IsFalse(File.Exists(oldFileShouldBeDeleted));
            Assert.IsTrue(Directory.Exists(newZipDestination));
        }

        [Test]
        public void Should_Do_Nothing_If_Version_Has_Not_Changed()
        {
            var zipDestination = toolFolder.EnsureUnzipped(appDataFolder.FullName,"ToolFolder", new ZipDetails { Version = "3.0", Path = "zipPath" });
            toolFolder.EnsureUnzipped(appDataFolder.FullName,"ToolFolder", new ZipDetails { Version = "3.0", Path = "zipPath" });
            mocker.Verify<IZipFile>(z => z.ExtractToDirectory("zipPath", zipDestination),Times.Once());
            Assert.IsTrue(Directory.Exists(zipDestination));
        }
        
    }
}