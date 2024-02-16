using AutoMoq;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class ContainingCodeTrackedLinesBuilder_Tests
    {
        [Test]
        public void Should_Create_ContainingCodeTracker_For_Each_Line_When_CPP()
        {
            // var autoMoqer = new AutoMoqer();

            // var textSnapshot = new Mock<ITextSnapshot>().Object;
            // var lines = new List<ILine>
            // {
            //     new Mock<ILine>().Object,
            //     new Mock<ILine>().Object
            // };
            // var containingCodeTrackers = new List<IContainingCodeTracker>
            // {
            //     new Mock<IContainingCodeTracker>().Object,
            //     new Mock<IContainingCodeTracker>().Object
            // };

            // var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ILinesContainingCodeTrackerFactory>();
            // mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
            //     containingCodeTrackerFactory.Create(textSnapshot, lines[0])
            // ).Returns(containingCodeTrackers[0]);
            // mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
            //    containingCodeTrackerFactory.Create(textSnapshot, lines[1])
            //).Returns(containingCodeTrackers[1]);

            // var expectedTrackedLines = new Mock<ITrackedLines>().Object;
            // var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            // mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(containingCodeTrackers)
            //            ).Returns(expectedTrackedLines);

            // var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            // var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, textSnapshot, Language.CPP);

            // Assert.That(trackedLines, Is.EqualTo(expectedTrackedLines));
            throw new System.NotImplementedException();

        }

        [Test]
        public void Should_Create_With_Empty_ContainingCodeTrackers_When_No_Lines()
        {
            //var autoMoqer = new AutoMoqer();

            //var textSnapshot = new Mock<ITextSnapshot>().Object;
            //var lines = new FileLineCoverage().GetLines("", 0, 0).ToList();

            //var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            //containingCodeTrackedLinesBuilder.Create(lines, textSnapshot, Language.CPP);

            //autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>().Verify(
            //    containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
            //        It.Is<List<IContainingCodeTracker>>(containingCodeTrackers => containingCodeTrackers.Count == 0)
            //    ), Times.Once);
            throw new System.NotImplementedException();
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        internal class TrackerArgs : IContainingCodeTracker
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public ILine Line { get; }
            public List<ILine> LinesInRange { get; }
            public CodeSpanRange CodeSpanRange { get; }
            public ITextSnapshot Snapshot { get; }
            public TrackerArgs(ITextSnapshot textSnapsot, ILine line)
            {
                Line = line;
                Snapshot = textSnapsot;
            }

            public static TrackerArgs Single(ILine line)
            {
                return new TrackerArgs(null, line);
            }

            public static TrackerArgs Range(List<ILine> lines, CodeSpanRange codeSpanRange)
            {
                return new TrackerArgs(null, lines, codeSpanRange);
            }

            public TrackerArgs(ITextSnapshot textSnapsot, List<ILine> lines, CodeSpanRange codeSpanRange)
            {
                Snapshot = textSnapsot;
                LinesInRange = lines;
                CodeSpanRange = codeSpanRange;
            }

            public override bool Equals(object obj)
            {
                var otherTrackerArgs = obj as TrackerArgs;
                if (Line != null)
                {
                    return Line == otherTrackerArgs.Line;
                }
                else
                {
                    var codeSpanRangeSame = CodeSpanRange.StartLine == otherTrackerArgs.CodeSpanRange.StartLine && CodeSpanRange.EndLine == otherTrackerArgs.CodeSpanRange.EndLine;
                    var linesSame = LinesInRange.Count == otherTrackerArgs.LinesInRange.Count;
                    if (linesSame)
                    {
                        for (var i = 0; i < LinesInRange.Count; i++)
                        {
                            if (LinesInRange[i] != otherTrackerArgs.LinesInRange[i])
                            {
                                linesSame = false;
                                break;
                            }
                        }
                    }
                    return codeSpanRangeSame && linesSame;
                }
            }

            public IEnumerable<IDynamicLine> Lines => throw new System.NotImplementedException();

            public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
            {
                throw new System.NotImplementedException();
            }
        }

        [TestCaseSource(typeof(RoslynDataClass), nameof(RoslynDataClass.TestCases))]
        public void Should_Create_ContainingCodeTrackers_In_Order_Contained_Lines_And_Single_Line_When_Roslyn_Languages
        (
            List<CodeSpanRange> codeSpanRanges,
            List<ILine> lines,
            List<TrackerArgs> expected
        )
        {
            //var autoMoqer = new AutoMoqer();
            //autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            //var mockTextSnapshot = new Mock<ITextSnapshot>();
            //var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            //var textSpans = codeSpanRanges.Select(codeSpanRange => new TextSpan(codeSpanRange.StartLine, codeSpanRange.EndLine - codeSpanRange.StartLine)).ToList();
            //mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object)).ReturnsAsync(textSpans);
            //textSpans.ForEach(textSpan =>
            //{
            //    mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.Start)).Returns(textSpan.Start);
            //    mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.End)).Returns(textSpan.End);
            //});

            //var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ILinesContainingCodeTrackerFactory>();
            //mockContainingCodeTrackerFactory.Setup(containingCodeFactory => containingCodeFactory.Create(
            //    It.IsAny<ITextSnapshot>(), It.IsAny<ILine>())
            //).Returns<ITextSnapshot, ILine>((snapshot, line) =>
            //{
            //    return new TrackerArgs(snapshot, line);
            //});
            //mockContainingCodeTrackerFactory.Setup(containingCodeFactory => containingCodeFactory.Create(
            //    It.IsAny<ITextSnapshot>(), It.IsAny<List<ILine>>(), It.IsAny<CodeSpanRange>())
            //).Returns<ITextSnapshot, List<ILine>, CodeSpanRange>((snapshot, ls, range) =>
            //{
            //    return new TrackerArgs(snapshot, ls, range);
            //});

            //var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            //mockContainingCodeTrackedLinesFactory.Setup(IContainingCodeTrackedLinesFactory => IContainingCodeTrackedLinesFactory.Create(It.IsAny<List<IContainingCodeTracker>>()))
            //    .Callback<List<IContainingCodeTracker>>(containingCodeTrackers =>
            //    {
            //        var invocationArgs = containingCodeTrackers.Select(t => t as TrackerArgs).ToList();
            //        Assert.True(invocationArgs.Select(args => args.Snapshot).All(snapshot => snapshot == mockTextSnapshot.Object));
            //        Assert.That(invocationArgs, Is.EqualTo(expected));
            //    });

            //var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            //containingCodeTrackedLinesBuilder.Create(lines, mockTextSnapshot.Object, Language.CSharp);


            //mockContainingCodeTrackedLinesFactory.VerifyAll();
            throw new System.NotImplementedException();
        }

        public class RoslynDataClass
        {
            public class RoslynTestCase : TestCaseData
            {
                public RoslynTestCase
                (
                    List<CodeSpanRange> codeSpanRanges,
                    List<ILine> lines,
                    List<TrackerArgs> expected

                ) : base(codeSpanRanges, lines, expected)
                {

                }
            }
            private static ILine GetLine(int lineNumber)
            {
                var mockLine = new Mock<ILine>();
                mockLine.Setup(line => line.Number).Returns(lineNumber);
                return mockLine.Object;
            }
            public static IEnumerable<RoslynTestCase> TestCases
            {
                get
                {
                    var test1CodeSpanRanges = new List<CodeSpanRange>
                    {
                        new CodeSpanRange(0,10),
                        new CodeSpanRange(20,30)
                    };
                    var test1Lines = new List<ILine>
                        {
                            GetLine(5),
                            GetLine(6),

                            GetLine(25),
                            GetLine(26),
                        };

                    yield return new RoslynTestCase(
                        test1CodeSpanRanges,
                        test1Lines,
                        new List<TrackerArgs>
                        {
                            TrackerArgs.Range(new List<ILine>{ test1Lines[0], test1Lines[1] }, test1CodeSpanRanges[0]),
                            TrackerArgs.Range(new List<ILine>{ test1Lines[2], test1Lines[3] }, test1CodeSpanRanges[1])
                        });

                    var test2CodeSpanRanges = new List<CodeSpanRange>
                    {
                        new CodeSpanRange(10,20),
                        new CodeSpanRange(25,40),
                        new CodeSpanRange(60,70),
                    };

                    var test2Lines = new List<ILine>
                    {
                        GetLine(5),//single
                        GetLine(6),// single

                        GetLine(15),// range

                        GetLine(45),//skip

                        GetLine(65),// range
                    };
                    yield return new RoslynTestCase(test2CodeSpanRanges, test2Lines, new List<TrackerArgs>
                    {
                        TrackerArgs.Single(test2Lines[0]),
                        TrackerArgs.Single(test2Lines[1]),
                        TrackerArgs.Range(new List<ILine>{ test2Lines[2] }, test2CodeSpanRanges[0]),
                        TrackerArgs.Single(test2Lines[3]),
                        TrackerArgs.Range(new List < ILine > { test2Lines[4] }, test2CodeSpanRanges[2]),
                    });

                    var test3CodeSpanRanges = new List<CodeSpanRange>
                    {
                        new CodeSpanRange(10,20),
                    };
                    var test3Lines = new List<ILine> { GetLine(21) }; // for line number adjustment
                    yield return new RoslynTestCase(test3CodeSpanRanges, test3Lines, new List<TrackerArgs>
                    {
                        TrackerArgs.Range(test3Lines, test3CodeSpanRanges[0])
                    });

                    var test4CodeSpanRanges = new List<CodeSpanRange>
                    {
                        new CodeSpanRange(10,20),
                    };
                    var test4Lines = new List<ILine> { GetLine(50) };
                    yield return new RoslynTestCase(test4CodeSpanRanges, test4Lines, new List<TrackerArgs>
                    {
                        TrackerArgs.Single(test4Lines[0])
                    });
                }
            }
        }
    }
}
