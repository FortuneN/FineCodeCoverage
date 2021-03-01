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
            coverletUtil.Initialize("folder path");
            mocker.Verify<ICoverletConsoleUtil>(g => g.Initialize("folder path"));
            mocker.Verify<ICoverletDataCollectorUtil>(dc => dc.Initialize("folder path"));
        }

        [TestCase(true,true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public async Task Should_Use_The_DataCollector_If_Possible(bool throwOnError, bool result)
        {
            var project = new Mock<ICoverageProject>().Object;

            var mockDataCollectorUtil = mocker.GetMock<ICoverletDataCollectorUtil>();
            mockDataCollectorUtil.Setup(dc => dc.CanUseDataCollector(project)).Returns(true);
            mockDataCollectorUtil.Setup(dc => dc.RunAsync(throwOnError).Result).Returns(result);

            var success = await coverletUtil.RunCoverletAsync(project, throwOnError);
            
            Assert.AreEqual(result, success);
            mockDataCollectorUtil.VerifyAll();
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public async Task Should_Use_The_Global_Tool_If_Not_Possible(bool throwOnError, bool result)
        {
            var project = new Mock<ICoverageProject>().Object;

            var mockDataCollectorUtil = mocker.GetMock<ICoverletDataCollectorUtil>();
            mockDataCollectorUtil.Setup(dc => dc.CanUseDataCollector(project)).Returns(false);

            var mockGlobalUtil = mocker.GetMock<ICoverletConsoleUtil>();
            mockGlobalUtil.Setup(g => g.RunAsync(project, throwOnError).Result).Returns(result);

            var success = await coverletUtil.RunCoverletAsync(project, throwOnError);

            Assert.AreEqual(result, success);
            mockDataCollectorUtil.VerifyAll();
            mockGlobalUtil.VerifyAll();
        }
    }
}