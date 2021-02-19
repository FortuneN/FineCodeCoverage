using System.IO;
using AutoMoq;
using FineCodeCoverage.Engine.Coverlet;
using NUnit.Framework;

namespace Test
{
    public class CoverletDataCollectorUtil_Initialize_Tests
    {
        private AutoMoqer mocker;
        private CoverletDataCollectorUtil coverletDataCollector;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverletDataCollector = mocker.Create<CoverletDataCollectorUtil>();
        }


        [Test]
        public void Should_Ensure_Unzipped_And_Sets_The_Quoted_TestAdapterPathArg()
        {
            var zipDetails = new ZipDetails { Path = "path", Version = "version" };
            var mockCoverletCollectorZipProvider = mocker.GetMock<ICoverletCollectorZipProvider>();
            mockCoverletCollectorZipProvider.Setup(zp => zp.ProvideZip()).Returns(zipDetails);

            var mockCoverletCollectorFolder = mocker.GetMock<ICoverletCollectorFolder>();
            mockCoverletCollectorFolder.Setup(cf => cf.EnsureUnzipped("appdatafolder", zipDetails)).Returns("zipdestination");

            coverletDataCollector.Initialize("appdatafolder");
            Assert.AreEqual($@"""{Path.Combine("zipdestination", "build", "netstandard1.0")}""", coverletDataCollector.TestAdapterPathArg);

        }
    }
}