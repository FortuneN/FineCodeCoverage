using FineCodeCoverage.Impl;
using Moq;
using NUnit.Framework;
using System;

namespace FineCodeCoverageTests
{
    public class FontAndColorsInfo_Equatable_Tests
    {
        [Test]
        public void Should_Be_Equal_When_Bold_Same_And_IItemCoverageColours_Equals()
        {
            var fontAndColors = new FontAndColorsInfo(GetItemCoverageColours(true),true);
            var fontAndColorsEqual = new FontAndColorsInfo(null, true);

            Assert.IsTrue(fontAndColors.Equals(fontAndColorsEqual));
        }

        [Test]
        public void Should_Not_Be_Equal_If_Bold_Not_Equal()
        {
            var fontAndColors = new FontAndColorsInfo(GetItemCoverageColours(true), true);
            var fontAndColorsNotEqual = new FontAndColorsInfo(null, false);

            Assert.IsFalse(fontAndColors.Equals(fontAndColorsNotEqual));
        }

        [Test]
        public void Should_Not_Be_Equal_If_IItemCoverageColours_Not_Equal()
        {
            var fontAndColors = new FontAndColorsInfo(GetItemCoverageColours(false), true);
            var fontAndColorsNotEqual = new FontAndColorsInfo(null, true);

            Assert.IsFalse(fontAndColors.Equals(fontAndColorsNotEqual));
        }

        private IItemCoverageColours GetItemCoverageColours(bool equals)
        {
            var mockItemCoverageColoursEquals = new Mock<IItemCoverageColours>();
            var equatable = mockItemCoverageColoursEquals.As<IEquatable<IItemCoverageColours>>();
            equatable.Setup(e => e.Equals(It.IsAny<IItemCoverageColours>())).Returns(equals);
            return mockItemCoverageColoursEquals.Object;
        }
    }
}