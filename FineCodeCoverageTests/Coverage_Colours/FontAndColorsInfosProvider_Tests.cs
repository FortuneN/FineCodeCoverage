using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Impl;
using FineCodeCoverageTests.Test_helpers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    public class FontAndColorsInfosProvider_Tests
    {
        [Test]
        public void GetCoverageColours_If_Required()
        {
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var editorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
            mockFontsAndColorsHelper.Setup(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    editorTextMarkerFontAndColorCategory,
                    new List<string> { "Coverage Touched Area" , "Coverage Not Touched Area" , "Coverage Partially Touched Area" })
                ).ReturnsAsync(new List<IFontAndColorsInfo>
                {
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange),
                    FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
                });
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();

            var colours = fontAndColorsInfosProvider.GetCoverageColours();
            var coveredColours = colours.GetColour(CoverageType.Covered);
            Assert.That(coveredColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(coveredColours.Background, Is.EqualTo(Colors.Red));
            var unCoveredColours = colours.GetColour(CoverageType.NotCovered);
            Assert.That(unCoveredColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(unCoveredColours.Background, Is.EqualTo(Colors.Orange));
            var partiallyCoveredColours = colours.GetColour(CoverageType.Partial);
            Assert.That(partiallyCoveredColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(partiallyCoveredColours.Background, Is.EqualTo(Colors.Black));

            var previousColors = fontAndColorsInfosProvider.GetCoverageColours();

            Assert.That(previousColors,Is.SameAs(colours));
            Assert.That(mockFontsAndColorsHelper.Invocations.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetChangedFontAndColorsInfos_Should_Return_Just_Changes()
        {
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var editorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
            };
            var second = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Pink, Colors.Gray,false),
            };
            mockFontsAndColorsHelper.SetupSequence(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    editorTextMarkerFontAndColorCategory,
                    new List<string> { "Coverage Touched Area", "Coverage Not Touched Area", "Coverage Partially Touched Area" })
                ).ReturnsAsync(first).ReturnsAsync(second);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();

            var changed = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            Assert.That(changed.Count, Is.EqualTo(3));
            Assert.That(changed[CoverageType.Covered].IsBold, Is.True);
            Assert.That(changed[CoverageType.Covered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(changed[CoverageType.Covered].ItemCoverageColours.Background, Is.EqualTo(Colors.Red));
            Assert.That(changed[CoverageType.NotCovered].IsBold, Is.False);
            Assert.That(changed[CoverageType.NotCovered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(changed[CoverageType.NotCovered].ItemCoverageColours.Background, Is.EqualTo(Colors.Orange));
            Assert.That(changed[CoverageType.Partial].IsBold, Is.True);
            Assert.That(changed[CoverageType.Partial].ItemCoverageColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(changed[CoverageType.Partial].ItemCoverageColours.Background, Is.EqualTo(Colors.Black));

            changed = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            Assert.That(changed.Count, Is.EqualTo(1));
            var partialChange = changed[CoverageType.Partial];
            Assert.That(partialChange.IsBold, Is.False);
            Assert.That(partialChange.ItemCoverageColours.Foreground, Is.EqualTo(Colors.Pink));
            Assert.That(partialChange.ItemCoverageColours.Background, Is.EqualTo(Colors.Gray));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetChangedFontAndColorsInfos_Should_Raise_Event_When_Just_Changes(bool equal)
        {
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var editorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
            };
            var second = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Blue, Colors.Orange,equal),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Pink, Colors.Gray,equal),
            };
            mockFontsAndColorsHelper.SetupSequence(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    editorTextMarkerFontAndColorCategory,
                    new List<string> { "Coverage Touched Area", "Coverage Not Touched Area", "Coverage Partially Touched Area" })
                ).ReturnsAsync(first).ReturnsAsync(second);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();

            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(It.IsAny<CoverageColoursChangedMessage>(), null), Times.Exactly(equal ? 1 : 2));
        }

        [Test]
        public void GetFontAndColorsInfos_Should_Return_All()
        {
            var autoMoqer = new AutoMoqer();
            var mockFontsAndColorsHelper = autoMoqer.GetMock<IFontsAndColorsHelper>();
            var editorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
            var first = new List<IFontAndColorsInfo>
            {
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.Green, Colors.Red),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Blue, Colors.Orange),
                FontAndColorsInfoFactory.CreateFontAndColorsInfo(true, Colors.White, Colors.Black),
            };

            mockFontsAndColorsHelper.Setup(
                fontsAndColorsHelper => fontsAndColorsHelper.GetInfosAsync(
                    editorTextMarkerFontAndColorCategory,
                    new List<string> { "Coverage Touched Area", "Coverage Not Touched Area", "Coverage Partially Touched Area" })
                ).ReturnsAsync(first);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var fontAndColorsInfosProvider = autoMoqer.Create<FontAndColorsInfosProvider>();

            fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();

            var fontAndColorsInfos = fontAndColorsInfosProvider.GetFontAndColorsInfos();
            Assert.That(fontAndColorsInfos.Count, Is.EqualTo(3));
            Assert.That(fontAndColorsInfos[CoverageType.Covered].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[CoverageType.Covered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Green));
            Assert.That(fontAndColorsInfos[CoverageType.Covered].ItemCoverageColours.Background, Is.EqualTo(Colors.Red));
            Assert.That(fontAndColorsInfos[CoverageType.NotCovered].IsBold, Is.False);
            Assert.That(fontAndColorsInfos[CoverageType.NotCovered].ItemCoverageColours.Foreground, Is.EqualTo(Colors.Blue));
            Assert.That(fontAndColorsInfos[CoverageType.NotCovered].ItemCoverageColours.Background, Is.EqualTo(Colors.Orange));
            Assert.That(fontAndColorsInfos[CoverageType.Partial].IsBold, Is.True);
            Assert.That(fontAndColorsInfos[CoverageType.Partial].ItemCoverageColours.Foreground, Is.EqualTo(Colors.White));
            Assert.That(fontAndColorsInfos[CoverageType.Partial].ItemCoverageColours.Background, Is.EqualTo(Colors.Black));
        }
    }
}