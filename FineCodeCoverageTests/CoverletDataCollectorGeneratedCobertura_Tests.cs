using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Coverlet;
using Moq;
using Moq.Language.Flow;
using NUnit.Framework;

namespace Test
{
    public class CoverletDataCollectorGeneratedCobertura_Tests
    {
        private AutoMoqer mocker;
        private CoverletDataCollectorGeneratedCobertura coverletDataCollectorGeneratedCobertura;
        private FileInfo generatedCobertura;
        private Mock<IDirectoryFilePoller> mockDirectoryFilePoller;
        private IReturnsResult<IDirectoryFilePoller> pollerMockSetUp;
        private string coverageOutputFile;
        private const string generatedCoberturaText = "Generated cobertura !";

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverletDataCollectorGeneratedCobertura = mocker.Create<CoverletDataCollectorGeneratedCobertura>();
            generatedCobertura = CreateTemporyFile();
            File.WriteAllText(generatedCobertura.FullName, generatedCoberturaText);
            coverageOutputFile = Path.GetTempFileName();
            //necessary for MoveTo
            File.Delete(coverageOutputFile);

        }

        [TearDown]
        public void DeleteTempFiles()
        {
            generatedCobertura.Delete();
            if (File.Exists(coverageOutputFile))
            {
                File.Delete(coverageOutputFile);
            }
        }

        private FileInfo CreateTemporyFile()
        {
            return new FileInfo(Path.GetTempFileName());
        }

        private void SetUpPoller(bool finds)
        {
            mockDirectoryFilePoller = mocker.GetMock<IDirectoryFilePoller>();
            pollerMockSetUp = mockDirectoryFilePoller.Setup(poller => poller.PollAsync("coverageOutputFolder", "coverage.cobertura.xml", CoverletDataCollectorGeneratedCobertura.fileWaitMs, It.IsAny<Func<FileInfo[], FileInfo>>(), SearchOption.AllDirectories)).Returns(Task.FromResult(finds ? generatedCobertura : null));

        }

        private Task CorrectPathAsync()
        {
            return coverletDataCollectorGeneratedCobertura.CorrectPathAsync("coverageOutputFolder",coverageOutputFile);
        }

        [Test]
        public async Task Should_Poll_The_Coverage_Output_Folder_All_Directories_For_The_Cobertura_File()
        {
            SetUpPoller(true);
            await CorrectPathAsync();
            mockDirectoryFilePoller.VerifyAll();

        }

        [TestCase(true)] // should only be one
        [TestCase(false)]
        public async Task Should_Select_The_Last_Written_Cobertura(bool reverseOrder)
        {
            SetUpPoller(true);
            Func<FileInfo[], FileInfo> _filter = null;
            pollerMockSetUp.Callback<string, string, int, Func<FileInfo[], FileInfo>, SearchOption>((_, __, ___, filter, ____) =>
               {
                   _filter = filter;
               });
            await CorrectPathAsync();
            Thread.Sleep(1000);
            var cobertura2 = CreateTemporyFile();
            Assert.AreSame(cobertura2, _filter(reverseOrder ? new FileInfo[] { generatedCobertura, cobertura2 } : new FileInfo[] { cobertura2, generatedCobertura }));


        }

        [Test]
        public void Should_Throw_Exception_If_Cobertura_Is_Not_Generated_In_The_Timeout()
        {
            SetUpPoller(false);
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await CorrectPathAsync();
            }, "Data collector did not generate coverage.cobertura.xml");
        }

        [Test]
        public async Task Should_Move_And_Rename_The_Generated_Cobertura()
        {
            SetUpPoller(true);
            await CorrectPathAsync();
            Assert.AreEqual(generatedCoberturaText, File.ReadAllText(coverageOutputFile));

        }
    }
    
}