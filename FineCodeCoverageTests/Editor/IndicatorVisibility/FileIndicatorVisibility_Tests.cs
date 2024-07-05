using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.IndicatorVisibility;
using FineCodeCoverage.Output;
using FineCodeCoverageTests.Test_helpers;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.IndicatorVisibility
{
    internal class FileIndicatorVisibility_Tests
    {
        [Test]
        public void SHould_Export_As_Initializable()
        {
            ExportsInitializable.Should_Export_IInitializable(typeof(FileIndicatorVisibility));
        }

        [Test]
        public void Should_Add_Itself_As_EventAggregator_Listener()
        {
            var mockEventAggregator = new Mock<IEventAggregator>();

            var fileIndicatorVisibility = new FileIndicatorVisibility(mockEventAggregator.Object);

            mockEventAggregator.Verify(eventAggregator => eventAggregator.AddListener(fileIndicatorVisibility,null), Times.Once());
        }

        [Test]
        public void Should_Toggle_IsVisible_And_Raise_VisibilityChanged_When_Handle_ToggleCoverageIndicatorsMessage()
        {
            var fileIndicatorVisibility = new AutoMoqer().Create<FileIndicatorVisibility>();
            var visibilityChangedCount = 0;
            fileIndicatorVisibility.VisibilityChanged += (sender, args) => {
                visibilityChangedCount++;
            };
            Assert.True(fileIndicatorVisibility.IsVisible(""));
            fileIndicatorVisibility.Handle(new ToggleCoverageIndicatorsMessage());
            Assert.False(fileIndicatorVisibility.IsVisible(""));
            Assert.AreEqual(1, visibilityChangedCount);
            fileIndicatorVisibility.Handle(new ToggleCoverageIndicatorsMessage());
            Assert.True(fileIndicatorVisibility.IsVisible(""));
            Assert.AreEqual(2, visibilityChangedCount);

        }
    }
}
