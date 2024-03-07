using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.Test_helpers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DynamicCoverageManager_Tests
    {
        [Test]
        public void Should_Export_IInitializable()
        {
            ExportsInitializable.Should_Export_IInitializable(typeof(DynamicCoverageManager));
        }

        [Test]
        public void Should_Listen_To_NewCoverageLinesMessage()
        {
            var autoMocker = new AutoMoqer();
            var dynamicCoverageManager = autoMocker.Create<DynamicCoverageManager>();

            autoMocker.Verify<IEventAggregator>(e => e.AddListener(dynamicCoverageManager, null), Times.Once());
        }

        [Test]
        public void Manage_Should_Create_Singleton_IBufferLineCoverage()
        {
            var autoMocker = new AutoMoqer();
            var dynamicCoverageManager = autoMocker.Create<DynamicCoverageManager>();

            var mockTextInfo = new Mock<ITextInfo>();
            var previousBufferLineCoverage = new Mock<IBufferLineCoverage>().Object;
            var propertyCollection = new PropertyCollection();
            propertyCollection.GetOrCreateSingletonProperty(() => previousBufferLineCoverage);
            mockTextInfo.Setup(textInfo => textInfo.TextBuffer.Properties).Returns(propertyCollection);
            
            var bufferLineCoverage = dynamicCoverageManager.Manage(mockTextInfo.Object);
            
            Assert.That(bufferLineCoverage, Is.SameAs(previousBufferLineCoverage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Manage_Should_Create_Singleton_IBufferLineCoverage_With_Last_Coverage_And_Dependencies(bool hasLastCoverage)
        {
            var autoMocker = new AutoMoqer();
            var eventAggregator = autoMocker.GetMock<IEventAggregator>().Object;
            var trackedLinesFactory = autoMocker.GetMock<ITrackedLinesFactory>().Object;
            var dynamicCoverageManager = autoMocker.Create<DynamicCoverageManager>();
            IFileLineCoverage lastCoverage = null;
            if (hasLastCoverage)
            {
                lastCoverage = new Mock<IFileLineCoverage>().Object;
                dynamicCoverageManager.Handle(new NewCoverageLinesMessage { CoverageLines = lastCoverage});
            }

            var mockTextInfo = new Mock<ITextInfo>();
            var textView = new Mock<ITextView>().Object;
            var propertyCollection = new PropertyCollection();
            mockTextInfo.Setup(textInfo => textInfo.TextBuffer.Properties).Returns(propertyCollection);

            var newBufferLineCoverage = new Mock<IBufferLineCoverage>().Object; 
            var mockBufferLineCoverageFactory = autoMocker.GetMock<IBufferLineCoverageFactory>();
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(textDocument => textDocument.FilePath).Returns("filepath");
            mockBufferLineCoverageFactory.Setup(
                bufferLineCoverageFactory => bufferLineCoverageFactory.Create(lastCoverage, mockTextInfo.Object, eventAggregator,trackedLinesFactory))
                .Returns(newBufferLineCoverage);

           
            
            var bufferLineCoverage = dynamicCoverageManager.Manage(mockTextInfo.Object);

            Assert.That(bufferLineCoverage, Is.SameAs(newBufferLineCoverage));
        }
    }
}
