using FineCodeCoverage.Impl;
using NUnit.Framework;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    public class ItemCoverageColours_Equatable_Tests
    {
        [Test]
        public void Should_Be_Equal_When_Foreground_And_Background_Equals()
        {
            var itemCoverageColours = new ItemCoverageColours(Colors.Green, Colors.Red);
            var itemCoverageColoursEqual = new ItemCoverageColours(Colors.Green, Colors.Red);

            Assert.IsTrue(itemCoverageColours.Equals(itemCoverageColoursEqual));
        }

        [Test]
        public void Should_Not_Be_Equal_When_Foreground_Not_Equal()
        {
            var itemCoverageColours = new ItemCoverageColours(Colors.Green, Colors.Red);
            var itemCoverageColoursNotEqual = new ItemCoverageColours(Colors.Blue, Colors.Red);

            Assert.IsFalse(itemCoverageColours.Equals(itemCoverageColoursNotEqual));
        }

        [Test]
        public void Should_Not_Be_Equal_When_Background_Not_Equal()
        {
            var itemCoverageColours = new ItemCoverageColours(Colors.Green, Colors.Red);
            var itemCoverageColoursNotEqual = new ItemCoverageColours(Colors.Green, Colors.Blue);

            Assert.IsFalse(itemCoverageColours.Equals(itemCoverageColoursNotEqual));
        }
    }
}