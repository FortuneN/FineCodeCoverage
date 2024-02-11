using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DynamicCoverageManager_Tests
    {
        [Test]
        public void Should_Export_IInitializable()
        {
            var dynamicCoverageManagerType = typeof(DynamicCoverageManager);
            var exportsIInitializable = dynamicCoverageManagerType.GetCustomAttributes(typeof(ExportAttribute),false).Any(ea => (ea as ExportAttribute).ContractType == typeof(IInitializable));
            Assert.That(exportsIInitializable, Is.True);
            Assert.That(dynamicCoverageManagerType.GetInterfaces().Any(i => i == typeof(IInitializable)), Is.True);
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

            var mockTextBuffer = new Mock<ITextBuffer>();
            var previousBufferLineCoverage = new Mock<IBufferLineCoverage>().Object;
            var propertyCollection = new PropertyCollection();
            propertyCollection.GetOrCreateSingletonProperty(() => previousBufferLineCoverage);
            mockTextBuffer.Setup(textBuffer => textBuffer.Properties).Returns(propertyCollection);

            var bufferLineCoverage = dynamicCoverageManager.Manage(null, mockTextBuffer.Object, "");
            
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

            var mockTextBuffer = new Mock<ITextBuffer>();
            var textView = new Mock<ITextView>().Object;
            var propertyCollection = new PropertyCollection();
            mockTextBuffer.Setup(textBuffer => textBuffer.Properties).Returns(propertyCollection);

            var newBufferLineCoverage = new Mock<IBufferLineCoverage>().Object; 
            var mockBufferLineCoverageFactory = autoMocker.GetMock<IBufferLineCoverageFactory>();
            mockBufferLineCoverageFactory.Setup(
                bufferLineCoverageFactory => bufferLineCoverageFactory.Create(lastCoverage, new TextInfo(textView,mockTextBuffer.Object,"filepath"), eventAggregator,trackedLinesFactory))
                .Returns(newBufferLineCoverage);

           
            
            var bufferLineCoverage = dynamicCoverageManager.Manage(textView, mockTextBuffer.Object, "filepath");

            Assert.That(bufferLineCoverage, Is.SameAs(newBufferLineCoverage));
        }
    }
}
