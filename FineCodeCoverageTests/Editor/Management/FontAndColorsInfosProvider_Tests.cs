using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.TestHelpers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class FontAndColorsInfosProvider_Tests
    {

        class CoverageFontAndColorsCategoryItemNames : ICoverageFontAndColorsCategoryItemNames
        {
            public Guid Guid1 { get; }
            public Guid Guid2 { get; }
            public FontAndColorsCategoryItemName Covered => new FontAndColorsCategoryItemName("Covered", Guid1);

            public FontAndColorsCategoryItemName Dirty => new FontAndColorsCategoryItemName("Dirty", Guid2);

            public FontAndColorsCategoryItemName NewLines => new FontAndColorsCategoryItemName("NewLines", Guid2);

            public FontAndColorsCategoryItemName NotCovered => new FontAndColorsCategoryItemName("NotCovered", Guid1);

            public FontAndColorsCategoryItemName PartiallyCovered => new FontAndColorsCategoryItemName("PartiallyCovered", Guid1);

            public FontAndColorsCategoryItemName NotIncluded => new FontAndColorsCategoryItemName("NotIncluded", Guid2);

            public CoverageFontAndColorsCategoryItemNames(bool singleCategory)
            {
                if(singleCategory)
                {
                    Guid1 = Guid2 = Guid.NewGuid();
                }
                else
                {
                    Guid1 = Guid.NewGuid();
                    Guid2 = Guid.NewGuid();
                }
            }
        }

        [Test]
        public void Should_GetCoverageColours_Using_CoverageFontAndColorsCategoryItemNames_Only_If_Required()
        {
            var coverageFontAndColorsCategoryItemNames = new CoverageFontAndColorsCategoryItemNames(false);
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            mockFontsAndColorsHelper.Setup(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    coverageFontAndColorsCategoryItemNames.Guid1,
                    new List<string> { "Covered", "NotCovered", "PartiallyCovered" })
                ).ReturnsAsync(new List<IFontAndColorsInfo>
                {
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                });
            mockFontsAndColorsHelper.Setup(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    coverageFontAndColorsCategoryItemNames.Guid2,
                    new List<string> { "Dirty", "NewLines", "NotIncluded"})
                ).ReturnsAsync(new List<IFontAndColorsInfo>
                {
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Brown),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Pink, Colors.AliceBlue),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed),
                });
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();
            fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames;

            var colours = fontAndColorsInfosProvider.GetCoverageColours();
            var coveredColours = colours.GetColour(DynamicCoverageType.Covered);
            Assert.That(coveredColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(coveredColours.Background, Is.EqualTo(Colors.Red));
            var unCoveredColours = colours.GetColour(DynamicCoverageType.NotCovered);
            Assert.That(unCoveredColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(unCoveredColours.Background, Is.EqualTo(Colors.Orange));
            var partiallyCoveredColours = colours.GetColour(DynamicCoverageType.Partial);
            Assert.That(partiallyCoveredColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(partiallyCoveredColours.Background, Is.EqualTo(Colors.Black));
            var dirtyColours = colours.GetColour(DynamicCoverageType.Dirty);
            Assert.That(dirtyColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(dirtyColours.Background, Is.EqualTo(Colors.Brown));
            var newLinesColours = colours.GetColour(DynamicCoverageType.NewLine);
            Assert.That(newLinesColours.Foreground, Is.EqualTo(Colors.Pink));
            Assert.That(newLinesColours.Background, Is.EqualTo(Colors.AliceBlue));
            var notIncludedColours = colours.GetColour(DynamicCoverageType.NotIncluded);
            Assert.That(notIncludedColours.Foreground, Is.EqualTo(Colors.AntiqueWhite));
            Assert.That(notIncludedColours.Background, Is.EqualTo(Colors.IndianRed));

            var previousColors = fontAndColorsInfosProvider.GetCoverageColours();

            Assert.That(previousColors, Is.SameAs(colours));
            Assert.That(mockFontsAndColorsHelper.Invocations.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetChangedFontAndColorsInfos_Should_Return_Just_Changes()
        {
            var coverageFontAndColorsCategoryItemNames = new CoverageFontAndColorsCategoryItemNames(true);
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.AliceBlue),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Goldenrod, Colors.GreenYellow),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed),
            };
            var second = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Pink, Colors.Gray,false),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.AliceBlue),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Goldenrod, Colors.GreenYellow),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed),
            };
            mockFontsAndColorsHelper.SetupSequence(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    coverageFontAndColorsCategoryItemNames.Guid1,
                    new List<string> { "Covered", "NotCovered", "PartiallyCovered", "Dirty", "NewLines","NotIncluded" })
                ).ReturnsAsync(first).ReturnsAsync(second);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();
            fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames;
            var changed = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            Assert.That(changed.Count, Is.EqualTo(6));
            Assert.That(changed[DynamicCoverageType.Covered].IsBold, Is.True);
            Assert.That(changed[DynamicCoverageType.Covered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(changed[DynamicCoverageType.Covered].ItemCoverageColours.Background, Is.EqualTo(Colors.Red));
            Assert.That(changed[DynamicCoverageType.NotCovered].IsBold, Is.False);
            Assert.That(changed[DynamicCoverageType.NotCovered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(changed[DynamicCoverageType.NotCovered].ItemCoverageColours.Background, Is.EqualTo(Colors.Orange));
            Assert.That(changed[DynamicCoverageType.Partial].IsBold, Is.True);
            Assert.That(changed[DynamicCoverageType.Partial].ItemCoverageColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(changed[DynamicCoverageType.Partial].ItemCoverageColours.Background, Is.EqualTo(Colors.Black));
            Assert.That(changed[DynamicCoverageType.Dirty].IsBold, Is.True);
            Assert.That(changed[DynamicCoverageType.Dirty].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(changed[DynamicCoverageType.Dirty].ItemCoverageColours.Background, Is.EqualTo(Colors.AliceBlue));
            Assert.That(changed[DynamicCoverageType.NewLine].IsBold, Is.True);
            Assert.That(changed[DynamicCoverageType.NewLine].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Goldenrod));
            Assert.That(changed[DynamicCoverageType.NewLine].ItemCoverageColours.Background, Is.EqualTo(Colors.GreenYellow));
            Assert.That(changed[DynamicCoverageType.NotIncluded].IsBold, Is.False);
            Assert.That(changed[DynamicCoverageType.NotIncluded].ItemCoverageColours.Foreground, Is.EqualTo(Colors.AntiqueWhite));
            Assert.That(changed[DynamicCoverageType.NotIncluded].ItemCoverageColours.Background, Is.EqualTo(Colors.IndianRed));

            changed = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            Assert.That(changed.Count, Is.EqualTo(1));
            var partialChange = changed[DynamicCoverageType.Partial];
            Assert.That(partialChange.IsBold, Is.False);
            Assert.That(partialChange.ItemCoverageColours.Foreground, Is.EqualTo(Colors.Pink));
            Assert.That(partialChange.ItemCoverageColours.Background, Is.EqualTo(Colors.Gray));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetChangedFontAndColorsInfos_Should_Raise_Event_When_Just_Changes(bool equal)
        {
            var coverageFontAndColorsCategoryItemNames = new CoverageFontAndColorsCategoryItemNames(true);
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed),
            };
            var second = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Pink, Colors.Gray,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed, equal),
            };

            mockFontsAndColorsHelper.SetupSequence(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    coverageFontAndColorsCategoryItemNames.Guid1,
                    new List<string> { "Covered", "NotCovered", "PartiallyCovered", "Dirty", "NewLines", "NotIncluded" })
                ).ReturnsAsync(first).ReturnsAsync(second);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();
            fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames;

            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(It.IsAny<CoverageColoursChangedMessage>(), null), Times.Exactly(equal ? 1 : 2));
        }

        [Test]
        public void GetFontAndColorsInfos_Should_Return_All()
        {
            var coverageFontAndColorsCategoryItemNames = new CoverageFontAndColorsCategoryItemNames(true);
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.AliceBlue),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Goldenrod, Colors.GreenYellow),
                 FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.AntiqueWhite, Colors.IndianRed),
            };

            mockFontsAndColorsHelper.Setup(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    coverageFontAndColorsCategoryItemNames.Guid1,
                    new List<string> { "Covered", "NotCovered", "PartiallyCovered", "Dirty", "NewLines","NotIncluded" })
                ).ReturnsAsync(first);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();
            fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames;
            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();

            var fontAndColorsInfos = fontAndColorsInfosProvider.GetFontAndColorsInfos();
            Assert.That(fontAndColorsInfos.Count, Is.EqualTo(6));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Covered].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Covered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Covered].ItemCoverageColours.Background, Is.EqualTo(Colors.Red));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotCovered].IsBold, Is.False);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotCovered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotCovered].ItemCoverageColours.Background, Is.EqualTo(Colors.Orange));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Partial].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Partial].ItemCoverageColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Partial].ItemCoverageColours.Background, Is.EqualTo(Colors.Black));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Dirty].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Dirty].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.Dirty].ItemCoverageColours.Background, Is.EqualTo(Colors.AliceBlue));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NewLine].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NewLine].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Goldenrod));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NewLine].ItemCoverageColours.Background, Is.EqualTo(Colors.GreenYellow));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotIncluded].IsBold, Is.False);
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotIncluded].ItemCoverageColours.Foreground, Is.EqualTo(Colors.AntiqueWhite));
            Assert.That(fontAndColorsInfos[DynamicCoverageType.NotIncluded].ItemCoverageColours.Background, Is.EqualTo(Colors.IndianRed));
        }
    }
}