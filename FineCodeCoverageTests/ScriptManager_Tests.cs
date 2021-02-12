using System.Threading.Tasks;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Output;
using Moq;
using NUnit.Framework;

namespace Test
{
    public class ScriptManager_When_Called_Back_Window_External
    {
        private ScriptManager scriptManager;
        private Mock<ISourceFileOpener> sourceFileOpener;
        private Mock<IProcess> mockProcess;

        [SetUp]
        public void SetUp()
        {
            sourceFileOpener = new Mock<ISourceFileOpener>();
            mockProcess = new Mock<IProcess>();
            scriptManager = new ScriptManager(sourceFileOpener.Object, mockProcess.Object);
        }

        [Test]
        public void Buy_Me_A_Coffee_Should_Open_PayPal()
        {
            scriptManager.BuyMeACoffee();
            mockProcess.Verify(p => p.Start(ScriptManager.payPal));
        }

        [Test]
        public void LogIssueOrSuggestion_Should_Open_Github_Issues()
        {
            scriptManager.LogIssueOrSuggestion();
            mockProcess.Verify(p => p.Start(ScriptManager.githubIssues));
        }

        [Test]
        public void RateAndReview_Should_Open_Market_Place_Rate_And_Review()
        {
            scriptManager.RateAndReview();
            mockProcess.Verify(p => p.Start(ScriptManager.marketPlaceRateAndReview));
        }

        [Test]
        public void DocumentFocused_Should_Call_The_Focus_Callback()
        {
            var calledCallback = false;
            scriptManager.FocusCallback = () => calledCallback = true;
            scriptManager.DocumentFocused();
            Assert.True(calledCallback);
        }

        [Test]
        public async Task Should_Call_SourceFileOpender_When_OpenFile()
        {
            scriptManager.OpenFile("aname", "q.cname", 2, 3);
            await scriptManager.openFileTask;
            sourceFileOpener.Verify(engine => engine.OpenFileAsync("aname", "q.cname", 2, 3));
        }
    }
}