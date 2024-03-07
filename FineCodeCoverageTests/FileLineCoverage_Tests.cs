using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Engine.Model;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    internal class FileLineCoverage_Tests
    {
        private static ILine CreateLine(int lineNumber, CoverageType coverageType = CoverageType.Covered)
        {
            var mockLine = new Mock<ILine>();
            mockLine.Setup(l => l.Number).Returns(lineNumber);
            mockLine.Setup(l => l.CoverageType).Returns(coverageType);
            return mockLine.Object;
        }

        [TestCaseSource(nameof(Cases))]
        public void GetLines_Test(IEnumerable<int> lineNumbers, int startLineNumber, int endLineNumber, IEnumerable<int> expectedLineNumbers)
        {
            var fileLineCoverage = new FileLineCoverage();
            fileLineCoverage.Add("fp", lineNumbers.Select((n => CreateLine(n))));
            fileLineCoverage.Sort();

            var lines = fileLineCoverage.GetLines("fp", startLineNumber, endLineNumber);
            Assert.That(lines.Select(l => l.Number), Is.EqualTo(expectedLineNumbers));
        }

        [Test]
        public void Should_Get_Empty_Lines_For_File_Not_In_Report()
        {
            var fileLineCoverage = new FileLineCoverage();

            var lines = fileLineCoverage.GetLines("", 1, 2);

            Assert.That(lines, Is.Empty);
        }

        [Test]
        public void Should_Should_Not_Throw_When_File_Renamed_Not_In_Report()
        {
            var fileLineCoverage = new FileLineCoverage();
            fileLineCoverage.UpdateRenamed("old", "new");
        }

        [Test]
        public void Should_Rename_When_FileName_Changes()
        {
            var fileLineCoverage = new FileLineCoverage();
            var lines = new[] { CreateLine(1), CreateLine(2) };
            fileLineCoverage.Add("old", lines);
            AssertLines("old");
            
            fileLineCoverage.UpdateRenamed("old", "new");
            AssertLines("new");
            Assert.That(fileLineCoverage.GetLines("old"), Is.Empty);

            void AssertLines(string fileName)
            {
                var allLines = fileLineCoverage.GetLines(fileName);
                Assert.That(allLines, Is.EqualTo(lines));
            }
        }

        static readonly object[] Cases =
        {
            new object[] { new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20}, 19, 20, new int[]{ 19,20} },
            new object[] { new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20}, 12, 13, new int[]{ 12,13} },
            new object[] { new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20}, 6, 7, new int[]{ 6,7} },
            new object[] {Enumerable.Empty<int>(), 0, 4,Enumerable.Empty<int>() },
            new object[] { new int[] { 3,2,1}, 0, 4, new int[]{ 1,2,3} },
            new object[] { new int[] { 3,2,1}, 0, 3, new int[]{ 1,2,3} },
            new object[] { new int[] { 3,2,1}, 1, 2, new int[]{ 1,2} },
            new object[] { new int[] { 3,2,1}, 2, 2, new int[]{ 2} },
            new object[] { new int[] { 3,2,1}, 4, 5, Enumerable.Empty<int>() }
        };
    }
}
