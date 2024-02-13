using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Editor.Tagging.Classification;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.Tagging.Base
{
    internal class CoverageTypeFilterExceptions : CoverageTypeFilterBase
    {
        public override string TypeIdentifier => "";

        protected override bool Enabled(IAppOptions appOptions)
        {
            return true;
        }
        public Func<Dictionary<DynamicCoverageType, bool>> ShowLookup;
        protected override Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions)
        {
            return ShowLookup?.Invoke();
        }
    }

    internal class CoverageTypeFilterBase_Exception_Tests
    {
        [Test]
        public void Should_Throw_If_ShowLookup_Null()
        {
            var coverageTypeFilterExceptions = new CoverageTypeFilterExceptions();
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            appOptions.ShowEditorCoverage = true;

            Assert.Throws<InvalidOperationException>(() => coverageTypeFilterExceptions.Initialize(appOptions));

        }

        [Test]
        public void Should_Throw_If_Incomplete_ShowLookup()
        {
            var coverageTypeFilterExceptions = new CoverageTypeFilterExceptions();
            coverageTypeFilterExceptions.ShowLookup = () => new Dictionary<DynamicCoverageType, bool>
            {
                { DynamicCoverageType.Covered, true },
                { DynamicCoverageType.NotCovered, true }
            };
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            appOptions.ShowEditorCoverage = true;

            Assert.Throws<InvalidOperationException>(() => coverageTypeFilterExceptions.Initialize(appOptions));

        }

        [Test]
        public void Should_Throw_When_Comparing_Different_ICoverageTypeFilter_For_Changes()
        {
            var coverageTypeFilterExceptions = new CoverageTypeFilterExceptions();
            coverageTypeFilterExceptions.ShowLookup = () => new Dictionary<DynamicCoverageType, bool>
            {
                { DynamicCoverageType.Covered, true },
                { DynamicCoverageType.NotCovered, true },
                { DynamicCoverageType.Partial,true },
                { DynamicCoverageType.CoveredDirty, true },
                { DynamicCoverageType.NotCoveredDirty, true },
                { DynamicCoverageType.PartialDirty,true },
                { DynamicCoverageType.NewLine,true },
            };
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            appOptions.ShowEditorCoverage = true;

            var other = new CoverageClassificationFilter();
            Assert.Throws<ArgumentException>(() => coverageTypeFilterExceptions.Changed(other));
        }
    }
}