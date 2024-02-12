using System;
using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Engine.Model;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    internal class FileLineCoverage_Tests
    {
       [TestCaseSource(nameof(Cases))]
        public void GetLines_Test(IEnumerable<int> lineNumbers, int startLineNumber, int endLineNumber, IEnumerable<int> expectedLineNumbers)
        {
            var fileLineCoverage = new FileLineCoverage();
            fileLineCoverage.Add("fp", lineNumbers.Select(n => new FineCodeCoverage.Engine.Cobertura.Line { Number = n }));
            fileLineCoverage.Completed();

            var lines = fileLineCoverage.GetLines("fp", startLineNumber, endLineNumber);
            Assert.That(lines.Select(l => l.Number), Is.EqualTo(expectedLineNumbers));
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
