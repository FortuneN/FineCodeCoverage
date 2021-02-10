using System.Collections.Generic;
using AutoMoq;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Engine.ReportGenerator;
using NUnit.Framework;

namespace Test
{
    public class FCCEngine_Tests
    {
        private AutoMoqer mocker;
        private FCCEngine fccEngine;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            fccEngine = mocker.Create<FCCEngine>();
        }
        [Test]
        public void Should_Initialize_AppFolder_Then_Utils()
        {
            List<int> callOrder = new List<int>();

            var appDataFolderPath = "some path";
            var mockAppDataFolder = mocker.GetMock<IAppDataFolder>();
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.Initialize()).Callback(() => callOrder.Add(1));
            mockAppDataFolder.Setup(appDataFolder => appDataFolder.DirectoryPath).Returns(appDataFolderPath);

            var coverletMock = mocker.GetMock<ICoverletUtil>().Setup(coverlet => coverlet.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(2));

            var reportGeneratorMock = mocker.GetMock<IReportGeneratorUtil>().Setup(reportGenerator => reportGenerator.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(3));

            var msTestPlatformMock = mocker.GetMock<IMsTestPlatformUtil>().Setup(msTestPlatform => msTestPlatform.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(4));

            var openCoverMock = mocker.GetMock<IOpenCoverUtil>().Setup(openCover => openCover.Initialize(appDataFolderPath)).Callback(() => callOrder.Add(5));

            fccEngine.Initialize();

            Assert.AreEqual(5, callOrder.Count);
            Assert.AreEqual(1, callOrder[0]);
        }
    }
}