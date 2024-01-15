using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverageTests
{
    internal class FirstTimeToolWindowOpener_Tests
    {
        private AutoMoqer mocker;
        private FirstTimeToolWindowOpener firstTimeToolWindowOpener;

        [SetUp]
        public void   SetUp()  {
            mocker = new AutoMoqer();
            firstTimeToolWindowOpener = mocker.Create<FirstTimeToolWindowOpener>();
        }

        [TestCase(true,false,true)]
        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        public async Task It_Should_Open_If_Have_Never_Shown_The_ToolWindow_And_InitializedFromTestContainerDiscoverer(
            bool initializedFromTestContainerDiscoverer,
            bool hasShownToolWindow,
            bool expectedShown
            )
        {
            mocker.GetMock<IInitializedFromTestContainerDiscoverer>().Setup(x => x.InitializedFromTestContainerDiscoverer).Returns(initializedFromTestContainerDiscoverer);
            mocker.GetMock<IShownToolWindowHistory>().Setup(x => x.HasShownToolWindow).Returns(hasShownToolWindow);

            await firstTimeToolWindowOpener.OpenIfFirstTimeAsync(CancellationToken.None);

            var expectedTimes = expectedShown ? Times.Once() : Times.Never();
            mocker.Verify<IToolWindowOpener>(toolWindowOpener => toolWindowOpener.OpenToolWindowAsync(), expectedTimes);

        }
    }
}
