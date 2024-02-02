using FineCodeCoverage.Impl;
using Moq;
using System;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    internal static class FontAndColorsInfoFactory
    {
        public static IFontAndColorsInfo CreateFontAndColorsInfo(bool bold, Color foreground = default, Color background = default,bool equals = true)
        {
            var mockItemCoverageColours = new Mock<IItemCoverageColours>();
            mockItemCoverageColours.SetupGet(itemCoverageColours => itemCoverageColours.Foreground).Returns(foreground);
            mockItemCoverageColours.SetupGet(itemCoverageColours => itemCoverageColours.Background).Returns(background);
            var mockFontAndColorsInfo = new Mock<IFontAndColorsInfo>();
            var mockEquatable = mockFontAndColorsInfo.As<IEquatable<IFontAndColorsInfo>>();
            mockEquatable.Setup(equatable=> equatable.Equals(It.IsAny<IFontAndColorsInfo>())).Returns(equals); 
            mockFontAndColorsInfo.SetupGet(fontAndColorsInfo => fontAndColorsInfo.IsBold).Returns(bold);
            mockFontAndColorsInfo.SetupGet(fontAndColorsInfo => fontAndColorsInfo.ItemCoverageColours).Returns(mockItemCoverageColours.Object);
            return mockFontAndColorsInfo.Object;
        }
    }
}