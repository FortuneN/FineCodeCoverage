using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class CoverletUtil_Tests
    {
        private AutoMoqer mocker;
        private CoverletUtil coverletUtil;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverletUtil = mocker.Create<CoverletUtil>();
        }
        [Test]
        public void Should_Initialize_The_GlobalTool_And_DataCollector()
        {
            var ct = CancellationToken.None;
            coverletUtil.Initialize("folder path",ct);
            mocker.Verify<ICoverletConsoleUtil>(g => g.Initialize("folder path",ct));
            mocker.Verify<ICoverletDataCollectorUtil>(dc => dc.Initialize("folder path", ct));
        }

        [Test]
        public async Task Should_Use_The_DataCollector_If_Possible()
        {
            var ct = CancellationToken.None;
            var project = new Mock<ICoverageProject>().Object;

            var mockDataCollectorUtil = mocker.GetMock<ICoverletDataCollectorUtil>();
            mockDataCollectorUtil.Setup(dc => dc.CanUseDataCollector(project)).Returns(true);
            mockDataCollectorUtil.Setup(dc => dc.RunAsync(ct));

            await coverletUtil.RunCoverletAsync(project,ct);
            
            mockDataCollectorUtil.VerifyAll();
        }

        [Test]
        public async Task Should_Use_The_Global_Tool_If_Not_Possible()
        {
            var ct = CancellationToken.None;
            var project = new Mock<ICoverageProject>().Object;

            var mockDataCollectorUtil = mocker.GetMock<ICoverletDataCollectorUtil>();
            mockDataCollectorUtil.Setup(dc => dc.CanUseDataCollector(project)).Returns(false);

            var mockGlobalUtil = mocker.GetMock<ICoverletConsoleUtil>();
            mockGlobalUtil.Setup(g => g.RunAsync(project, ct));

            await coverletUtil.RunCoverletAsync(project,ct);

            mockDataCollectorUtil.VerifyAll();
            mockGlobalUtil.VerifyAll();
        }
    }
}