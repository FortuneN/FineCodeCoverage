using AutoMoq;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class CoverageFontAndColorsCategoryItemNames_Tests
    {
        [Test]
        public void Should_Use_MEF_Category_And_FCCEditorFormatDefinitionNames_When_Vs_Does_Not_Have_Coverage_Markers()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IVsHasCoverageMarkersLogic, bool>(x => x.HasCoverageMarkers()).Returns(false);
            
            Verify_Use_MEF_Category_And_FCCEditorFormatDefinitionNames(autoMoqer);
        }

        [Test]
        public void Should_Use_MEF_Category_And_FCCEditorFormatDefinitionNames_When_Vs_Does_Have_Coverage_Markers_But_Not_UseEnterpriseFontsAndColors()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IVsHasCoverageMarkersLogic, bool>(x => x.HasCoverageMarkers()).Returns(true);
            autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(appOptionsProvider => appOptionsProvider.Get()).Returns(new Mock<IAppOptions>().Object);
            
            Verify_Use_MEF_Category_And_FCCEditorFormatDefinitionNames(autoMoqer);
        }

        [Test]
        public void Should_Use_MEF_Category_For_Non_Markers_When_UseEnterpriseFontsAndColors()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IVsHasCoverageMarkersLogic, bool>(x => x.HasCoverageMarkers()).Returns(true);
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.UseEnterpriseFontsAndColors).Returns(true);
            autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);

            var coverageFontAndColorsCategoryItemNamesManager = CreateAndInitialize(autoMoqer);

            var categoryItemNames = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;

            AssertNonMarkers(categoryItemNames);
        }

        [Test]
        public void Should_Use_VS_For_Markers_When_UseEnterpriseFontsAndColors()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IVsHasCoverageMarkersLogic, bool>(x => x.HasCoverageMarkers()).Returns(true);
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.UseEnterpriseFontsAndColors).Returns(true);
            autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);

            var coverageFontAndColorsCategoryItemNamesManager = CreateAndInitialize(autoMoqer);

            var categoryItemNames = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;

            AssertVSMarkers(categoryItemNames);
        }

        [TestCase(false,true, false, true)]
        [TestCase(false, false, false, true)]
        [TestCase(true,false, true, false)]
        [TestCase(true, true, true, true)]
        public void Change_Test(bool hasCoverageMarkers,bool useEnterpriseFontsAndColors, bool expectedChangedRaised, bool expectedMEF)
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IVsHasCoverageMarkersLogic, bool>(x => x.HasCoverageMarkers()).Returns(hasCoverageMarkers);
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.UseEnterpriseFontsAndColors).Returns(useEnterpriseFontsAndColors);
            var mockChangedAppOptions = new Mock<IAppOptions>();
            mockChangedAppOptions.SetupGet(appOptions => appOptions.UseEnterpriseFontsAndColors).Returns(!useEnterpriseFontsAndColors);
            var mockAppOptionsProvider = autoMoqer.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);

            var coverageFontAndColorsCategoryItemNamesManager = CreateAndInitialize(autoMoqer);

            var changedRaised = false;
            coverageFontAndColorsCategoryItemNamesManager.Changed += (sender, args) =>
            {
                changedRaised = true;
            };

            var _ = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, mockChangedAppOptions.Object);

            var changed = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;
            var changedCovered = changed.Covered;
            var changedNotCovered = changed.NotCovered;
            var changedPartiallyCovered = changed.PartiallyCovered;

            
            AssertNonMarkers(changed);
            Assert.That(changedRaised, Is.EqualTo(expectedChangedRaised));
            if (expectedMEF)
            {
                AssertMEFMarkers(changed);
            }
            else
            {
                AssertVSMarkers(changed);
            }

        }

        private CoverageFontAndColorsCategoryItemNamesManager CreateAndInitialize(AutoMoqer autoMoqer)
        {
            var coverageFontAndColorsCategoryItemNamesManager = autoMoqer.Create<CoverageFontAndColorsCategoryItemNamesManager>();
            coverageFontAndColorsCategoryItemNamesManager.Initialize(new FCCEditorFormatDefinitionNames(
                "Covered",
                "NotCovered",
                "PartiallyCovered",
                "NewLines",
                "Dirty"));
            return coverageFontAndColorsCategoryItemNamesManager;
        }

        private void Verify_Use_MEF_Category_And_FCCEditorFormatDefinitionNames(AutoMoqer autoMoqer)
        {
            var coverageFontAndColorsCategoryItemNamesManager = CreateAndInitialize(autoMoqer);

            var categoryItemNames = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;
            AssertMEFMarkers(categoryItemNames);

            AssertNonMarkers(categoryItemNames);
        }

        private void AssertMEFMarkers(ICoverageFontAndColorsCategoryItemNames categoryItemNames)
        {
            AssertMarkers(categoryItemNames, true, "Covered", "NotCovered", "PartiallyCovered");
        }

        private void AssertVSMarkers(ICoverageFontAndColorsCategoryItemNames categoryItemNames)
        {
            AssertMarkers(categoryItemNames, false, MarkerTypeNames.Covered, MarkerTypeNames.NotCovered, MarkerTypeNames.PartiallyCovered);
        }

        private void AssertMarkers(
            ICoverageFontAndColorsCategoryItemNames categoryItemNames, 
            bool expectedMef,
            string expectedCoveredName,
            string expectedNotCoveredName,
            string expectedPartiallyCoveredName
            )
        {
            AssertCategory(new List<Guid> {
                categoryItemNames.Covered.Category,
                categoryItemNames.NotCovered.Category,
                categoryItemNames.PartiallyCovered.Category
            }, expectedMef);

            Assert.That(categoryItemNames.Covered.ItemName, Is.EqualTo(expectedCoveredName));
            Assert.That(categoryItemNames.NotCovered.ItemName, Is.EqualTo(expectedNotCoveredName));
            Assert.That(categoryItemNames.PartiallyCovered.ItemName, Is.EqualTo(expectedPartiallyCoveredName));

        }

        private void AssertNonMarkers(
            ICoverageFontAndColorsCategoryItemNames categoryItemNames
            )
        {
            AssertCategory(new List<Guid> {
                categoryItemNames.NewLines.Category,
                categoryItemNames.Dirty.Category,
            }, true);

            Assert.That(categoryItemNames.NewLines.ItemName, Is.EqualTo("NewLines"));
            Assert.That(categoryItemNames.Dirty.ItemName, Is.EqualTo("Dirty"));
        }

        private void AssertCategory(List<Guid> categories, bool expectedMef)
        {
            var editorMEFCategory = new Guid("75A05685-00A8-4DED-BAE5-E7A50BFA929A");
            var editorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
            var expectedCategory = expectedMef ? editorMEFCategory : editorTextMarkerFontAndColorCategory;
            categories.ForEach(category => Assert.That(category, Is.EqualTo(expectedCategory)));
        }

    }
}