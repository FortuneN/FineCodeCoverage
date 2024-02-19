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
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class ContainingCodeTrackedLinesBuilder_Tests
    {
        [Test]
        public void Should_Create_ContainingCodeTracker_For_Each_Line_When_CPP()
        {
            var autoMoqer = new AutoMoqer();

            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var lines = new List<ILine>
             {
                 new Mock<ILine>().Object,
                 new Mock<ILine>().Object
             };
            var containingCodeTrackers = new List<IContainingCodeTracker>
             {
                 new Mock<IContainingCodeTracker>().Object,
                 new Mock<IContainingCodeTracker>().Object
             };

            var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ILinesContainingCodeTrackerFactory>();
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
                containingCodeTrackerFactory.Create(textSnapshot, lines[0], SpanTrackingMode.EdgeExclusive)
            ).Returns(containingCodeTrackers[0]);
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
               containingCodeTrackerFactory.Create(textSnapshot, lines[1], SpanTrackingMode.EdgeExclusive)
           ).Returns(containingCodeTrackers[1]);

            var expectedTrackedLines = new Mock<ITrackedLines>().Object;
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(containingCodeTrackers,null)
                       ).Returns(expectedTrackedLines);

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, textSnapshot, Language.CPP);

            Assert.That(trackedLines, Is.EqualTo(expectedTrackedLines));

        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        internal class TrackerArgs : IContainingCodeTracker
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public ILine Line { get; }
            public List<ILine> LinesInRange { get; }
            public CodeSpanRange CodeSpanRange { get; }
            public ITextSnapshot Snapshot { get; }
            public SpanTrackingMode SpanTrackingMode { get; }
            public TrackerArgs(ITextSnapshot textSnapshot, ILine line, SpanTrackingMode spanTrackingMode)
            {
                Line = line;
                Snapshot = textSnapshot;
                SpanTrackingMode = spanTrackingMode;
            }

            public static TrackerArgs ExpectedSingle(ILine line, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(null, line,spanTrackingMode);
            }

            public static TrackerArgs ExpectedRange(List<ILine> lines, CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(null, lines, codeSpanRange, spanTrackingMode);
            }

            public TrackerArgs(ITextSnapshot textSnapsot, List<ILine> lines, CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                Snapshot = textSnapsot;
                LinesInRange = lines;
                CodeSpanRange = codeSpanRange;
                SpanTrackingMode = spanTrackingMode;
            }

            public override bool Equals(object obj)
            {
                var otherTrackerArgs = obj as TrackerArgs;
                var spanTrackingModeSame = SpanTrackingMode == otherTrackerArgs.SpanTrackingMode;
                if (Line != null)
                {
                    return Line == otherTrackerArgs.Line &&spanTrackingModeSame;
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
                    return codeSpanRangeSame && linesSame && spanTrackingModeSame;
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
            List<TrackerArgs> expected,
            Action<Mock<ITextSnapshot>> setUpTextSnapshotForOtherLines
        )
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            setUpTextSnapshotForOtherLines(mockTextSnapshot);
            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            var textSpans = codeSpanRanges.Select(codeSpanRange => new TextSpan(codeSpanRange.StartLine, codeSpanRange.EndLine - codeSpanRange.StartLine)).ToList();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object)).ReturnsAsync(textSpans);
            textSpans.ForEach(textSpan =>
            {
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.Start)).Returns(textSpan.Start);
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.End)).Returns(textSpan.End);
            });

            var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ILinesContainingCodeTrackerFactory>();
            mockContainingCodeTrackerFactory.Setup(containingCodeFactory => containingCodeFactory.Create(
                It.IsAny<ITextSnapshot>(), It.IsAny<ILine>(), It.IsAny<SpanTrackingMode>())
            ).Returns<ITextSnapshot, ILine, SpanTrackingMode>((snapshot, line, spanTrackingMode) =>
            {
                return new TrackerArgs(snapshot, line, spanTrackingMode);
            });
            mockContainingCodeTrackerFactory.Setup(containingCodeFactory => containingCodeFactory.Create(
                It.IsAny<ITextSnapshot>(), It.IsAny<List<ILine>>(), It.IsAny<CodeSpanRange>(),It.IsAny<SpanTrackingMode>())
            ).Returns<ITextSnapshot, List<ILine>, CodeSpanRange,SpanTrackingMode>((snapshot, ls, range,spanTrackingMode) =>
            {
                return new TrackerArgs(snapshot, ls, range, spanTrackingMode);
            });

            var newCodeTracker = autoMoqer.GetMock<INewCodeTracker>().Object;
            autoMoqer.Setup<INewCodeTrackerFactory, INewCodeTracker>(newCodeTrackerFactory => newCodeTrackerFactory.Create(true)).Returns(newCodeTracker);

            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            mockContainingCodeTrackedLinesFactory.Setup(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(It.IsAny<List<IContainingCodeTracker>>(),newCodeTracker)
            ).Callback<List<IContainingCodeTracker>,INewCodeTracker>((containingCodeTrackers,_) =>
                {
                    var invocationArgs = containingCodeTrackers.Select(t => t as TrackerArgs).ToList();
                    Assert.True(invocationArgs.Select(args => args.Snapshot).All(snapshot => snapshot == mockTextSnapshot.Object));
                    Assert.That(invocationArgs, Is.EqualTo(expected));
                });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            containingCodeTrackedLinesBuilder.Create(lines, mockTextSnapshot.Object, Language.CSharp);


            mockContainingCodeTrackedLinesFactory.VerifyAll();
        }

        public class RoslynDataClass
        {
            public class RoslynTestCase : TestCaseData
            {
                public RoslynTestCase
                (
                    List<CodeSpanRange> codeSpanRanges,
                    List<ILine> lines,
                    List<TrackerArgs> expected,
                    Action<Mock<ITextSnapshot>> setUpTextSnapshotForOtherLines,
                    string testName = null

                ) : base(codeSpanRanges, lines, expected, setUpTextSnapshotForOtherLines)
                {
                    if (testName != null)
                    {
                        this.SetName(testName);
                    }
                   
                }
            }
            private static ILine GetLine(int lineNumber)
            {
                var mockLine = new Mock<ILine>();
                mockLine.Setup(line => line.Number).Returns(lineNumber);
                return mockLine.Object;
            }
            private static Action<Mock<ITextSnapshot>> ExcludingOtherLinesTextSnapshotSetup(int length)
            {
                return mockTextSnapshot =>
                {
                    mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(length);

                    var textSnapshotLine = SetupSnapshotLineText(mockTextSnapshot, "", 0);
                    mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromLineNumber(It.IsAny<int>())).Returns(textSnapshotLine);
                };
            }

            private static ITextSnapshotLine SetupSnapshotLineText(Mock<ITextSnapshot> mockTextSnapshot, string text,int identifier)
            {
                mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(1000);
                var mockTextSnapshotLine = new Mock<ITextSnapshotLine>();
                var textSpan = new Span(0, identifier);
                var snapshotSpan = new SnapshotSpan(mockTextSnapshot.Object, textSpan);
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetText(textSpan)).Returns(text);
                mockTextSnapshotLine.Setup(textSnapshotLine => textSnapshotLine.Extent).Returns(snapshotSpan);
                return mockTextSnapshotLine.Object;
            }

            private struct LineAndText
            {
                public int LineNumber { get; }
                public string LineText { get; }
                public LineAndText(int lineNumber, string lineText)
                {
                    LineNumber = lineNumber;
                    LineText = lineText;
                }
            }
            private static Action<Mock<ITextSnapshot>> SetupOtherLines(int length, IEnumerable<LineAndText> lineAndTexts)
            {
                return mockTextSnapshot =>
                {
                    mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(length);
                    var count = 0;
                    foreach(var lineAndText in lineAndTexts)
                    {
                        var textSnapshotLine = SetupSnapshotLineText(mockTextSnapshot, lineAndText.LineText, count);

                        mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromLineNumber(lineAndText.LineNumber))
                            .Returns(textSnapshotLine);
                        count++;
                    }
                };
            }

            public static IEnumerable<RoslynTestCase> TestCases
            {
                get
                {
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
                            TrackerArgs.ExpectedRange(new List<ILine>{ test1Lines[0], test1Lines[1] }, test1CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedRange(new List<ILine>{ test1Lines[2], test1Lines[3] }, test1CodeSpanRanges[1], SpanTrackingMode.EdgeExclusive)
                            }, 
                            ExcludingOtherLinesTextSnapshotSetup(30)
                            );
                    }

                    {
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
                            TrackerArgs.ExpectedSingle(test2Lines[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingle(test2Lines[1], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedRange(new List<ILine>{ test2Lines[2] }, test2CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            // this is the range that has not been included in code coverage - excluded 
                            TrackerArgs.ExpectedRange(new List<ILine>{}, test2CodeSpanRanges[1], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingle(test2Lines[3], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedRange(new List < ILine > { test2Lines[4] }, test2CodeSpanRanges[2], SpanTrackingMode.EdgeExclusive),
                        }, ExcludingOtherLinesTextSnapshotSetup(70));
                    }

                    {
                        var test3CodeSpanRanges = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(10,20),
                        };
                        var test3Lines = new List<ILine> { GetLine(21) }; // for line number adjustment
                        yield return new RoslynTestCase(test3CodeSpanRanges, test3Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedRange(test3Lines, test3CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive)
                        }, ExcludingOtherLinesTextSnapshotSetup(21));
                    }

                    {
                        var test4CodeSpanRanges = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(10,20),
                        };
                        var test4Lines = new List<ILine> { GetLine(50) };
                        yield return new RoslynTestCase(test4CodeSpanRanges, test4Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedRange(new List<ILine>(), test4CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingle(test4Lines[0], SpanTrackingMode.EdgeExclusive)
                        }, ExcludingOtherLinesTextSnapshotSetup(50));
                    }

                    {
                        var test5CodeSpanRanges  = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(5,20),
                        };
                        var test5Lines = new List<ILine> { GetLine(15) };
                        yield return new RoslynTestCase(test5CodeSpanRanges, test5Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedRange(new List<ILine>(), new CodeSpanRange(0,0), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedRange(new List<ILine>(), new CodeSpanRange(2,2), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedRange(test5Lines, test5CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedRange(new List<ILine>(), new CodeSpanRange(21,21), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedRange(new List<ILine>(), new CodeSpanRange(23,23), SpanTrackingMode.EdgeNegative),
                        }, SetupOtherLines(24, new List<LineAndText>
                        {
                            new LineAndText(0,"text"),
                            new LineAndText(1,""),
                            new LineAndText(2,"text"),
                            new LineAndText(3,""),
                            new LineAndText(4,""),
                            
                            new LineAndText(21,"text"),
                            new LineAndText(22,""),
                            new LineAndText(23,"text"),
                        }),"Other lines");
                    }
                }
            }
        }
    }
}
