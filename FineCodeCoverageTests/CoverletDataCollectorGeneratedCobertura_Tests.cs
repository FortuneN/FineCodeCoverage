using System;
using System.IO;
using System.Threading;
using FineCodeCoverage.Engine.Coverlet;
using NUnit.Framework;

namespace Test
{
    public class CoverletDataCollectorGeneratedCobertura_Tests
    {
        private DirectoryInfo coverageOutputFolder;

        [SetUp]
        public void CreateCoverageOutputFolder()
        {
            coverageOutputFolder = CreateTemporaryDirectory();
        }

        [TearDown]
        public void DeleteCoverageOutputFolder()
        {
            coverageOutputFolder.Delete(true);
        }

        public DirectoryInfo CreateTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
            
        }

        [Test]
        public void Should_Rename_And_Move_The_Generated()
        {
            CreateGeneratedFiles();
            var coverletDataCollectorGeneratedCobertura = new CoverletDataCollectorGeneratedCobertura();
            var coverageOutputFile = Path.Combine(coverageOutputFolder.FullName, "renamed.xml");
            coverletDataCollectorGeneratedCobertura.CorrectPath(coverageOutputFolder.FullName, coverageOutputFile);
            Assert.AreEqual("last", File.ReadAllText(coverageOutputFile));
        }

        [Test]
        public void Should_Delete_The_Generated_Directory()
        {
            var generatedDirectory = GetLastDirectoryPath();
            CreateGeneratedFiles();
            Assert.True(Directory.Exists(generatedDirectory));
            var coverletDataCollectorGeneratedCobertura = new CoverletDataCollectorGeneratedCobertura();
            var coverageOutputFile = Path.Combine(coverageOutputFolder.FullName, "renamed.xml");
            coverletDataCollectorGeneratedCobertura.CorrectPath(coverageOutputFolder.FullName, coverageOutputFile);
            Assert.False(Directory.Exists(generatedDirectory));
        }

        [Test]
        public void Should_Throw_If_Did_Not_Generate()
        {
            var coverletDataCollectorGeneratedCobertura = new CoverletDataCollectorGeneratedCobertura();
            var coverageOutputFile = Path.Combine(coverageOutputFolder.FullName, "renamed.xml");
            Assert.Throws<Exception>(() =>
            {
                coverletDataCollectorGeneratedCobertura.CorrectPath(coverageOutputFolder.FullName, coverageOutputFile);
            }, "Data collector did not generate coverage.cobertura.xml");
            
        }
        private string GetLastDirectoryPath()
        {
            return Path.Combine(coverageOutputFolder.FullName, "efgh");
        }
        private void CreateGeneratedFiles()
        {
            var firstDirectory = Path.Combine(coverageOutputFolder.FullName, "abcd");
            var lastDirectory = GetLastDirectoryPath();

            WriteGeneratedCobertura(firstDirectory, false);
            Thread.Sleep(2000);
            WriteGeneratedCobertura(lastDirectory, true);
        }
        private void WriteGeneratedCobertura(string directory, bool last)
        {
            Directory.CreateDirectory(directory);
            var generatedPath = Path.Combine(directory, CoverletDataCollectorGeneratedCobertura.collectorGeneratedCobertura);
            File.WriteAllText(generatedPath, last ? "last" : "first");
        }
        
    }
}