using System.IO;
using System.Threading;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
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
            var ct = CancellationToken.None;

            var mockToolFolder = mocker.GetMock<IToolUnzipper>();
            mockToolFolder.Setup(cf => cf.EnsureUnzipped("appdatafolder", "coverletCollector", "coverlet.collector",ct)).Returns("zipdestination");

            coverletDataCollector.Initialize("appdatafolder",ct);
            Assert.AreEqual($@"""{Path.Combine("zipdestination", "build", "netstandard1.0")}""", coverletDataCollector.TestAdapterPathArg);

        }
    }
}