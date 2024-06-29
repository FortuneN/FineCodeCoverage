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

        [Test]
        public void Should_Return_Distinct_Sorted_Lines()
        {
            var fileLineCoverage = new FileLineCoverage();
            fileLineCoverage.Add("file1", new[] { CreateLine(2), CreateLine(1), CreateLine(3),CreateLine(1) });
            fileLineCoverage.Sort();

            var lines = fileLineCoverage.GetLines("file1");
            
            Assert.That(lines.Select(l => l.Number), Is.EqualTo(new int[] { 1,2,3}));
        }

        [Test]
        public void Should_Get_Empty_Lines_For_File_Not_In_Report()
        {
            var fileLineCoverage = new FileLineCoverage();

            var lines = fileLineCoverage.GetLines("");

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
            fileLineCoverage.Sort();
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
    }
}
