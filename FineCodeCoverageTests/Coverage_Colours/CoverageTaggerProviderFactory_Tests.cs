using AutoMoq;
using Castle.Core.Internal;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverageTests
{
    internal class DummyLineSpanTagger : ILineSpanTagger<DummyTag>
    {
        public TagSpan<DummyTag> GetTagSpan(ILineSpan lineSpan)
        {
            throw new NotImplementedException();
        }
    }

    public class CoverageTaggerProviderFactory_Tests
    {
        [Test]
        public void Should_Export_IInitializable()
        {
            var coverageTaggerProviderFactoryType = typeof(CoverageTaggerProviderFactory);
            var exportsIInitializable = coverageTaggerProviderFactoryType.GetAttributes<ExportAttribute>().Any(ea => ea.ContractType == typeof(IInitializable));
            Assert.That(exportsIInitializable, Is.True);
            Assert.That(coverageTaggerProviderFactoryType.GetInterfaces().Any(i => i == typeof(IInitializable)), Is.True);
        }

        [Test]
        public void Should_Listen_For_NewCoverageLinesMessage()
        {
            var autoMoqer = new AutoMoqer();
            var coverageTaggerProviderFactory = autoMoqer.Create<CoverageTaggerProviderFactory>();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(coverageTaggerProviderFactory, null));
        }

        [Test]
        public void Should_Create_CoverageTaggerProvider_With_Existing_FileLineCoverage()
        {
            //var autoMoqer = new AutoMoqer();
            //var coverageTaggerProviderFactory = autoMoqer.Create<CoverageTaggerProviderFactory>();

            //var existingFileLineCoverage = new Mock<IFileLineCoverage>().Object;
            //coverageTaggerProviderFactory.Handle(new NewCoverageLinesMessage { CoverageLines = existingFileLineCoverage});

            //var coverageTaggerProvider = coverageTaggerProviderFactory.Create<DummyTag, DummyCoverageTypeFilter>(new DummyLineSpanTagger());

            //var fileLineCoverage = coverageTaggerProvider.GetType().GetField("lastCoverageLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(coverageTaggerProvider);
            //Assert.That(fileLineCoverage, Is.SameAs(existingFileLineCoverage));
            throw new NotImplementedException();
        }
    }
}