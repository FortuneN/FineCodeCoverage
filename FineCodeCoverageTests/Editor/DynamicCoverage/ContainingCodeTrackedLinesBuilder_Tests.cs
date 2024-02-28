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
    [TestFixture(true)]
    [TestFixture(false)]
    internal class ContainingCodeTrackedLinesBuilder_Tests
    {
        private readonly bool isCSharp;

        public ContainingCodeTrackedLinesBuilder_Tests(bool isCSharp)
        {
            this.isCSharp = isCSharp;
        }

        private static CodeSpanRange CodeSpanRangeFromLine(ILine line)
        {
            return CodeSpanRange.SingleLine(line.Number -1);
        }
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
            var firstLine = lines[0];
            var firstCodeSpanRange = CodeSpanRangeFromLine(firstLine);
            var secondLine = lines[1];
            var secondCodeSpanRange = CodeSpanRangeFromLine(secondLine);
            var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
                containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { firstLine },firstCodeSpanRange, SpanTrackingMode.EdgeExclusive)
            ).Returns(containingCodeTrackers[0]);
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
               containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { secondLine }, secondCodeSpanRange, SpanTrackingMode.EdgeExclusive)
           ).Returns(containingCodeTrackers[1]);

            var expectedTrackedLines = new TrackedLines(null, null);
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(containingCodeTrackers, null)
                       ).Returns(expectedTrackedLines);

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, textSnapshot, Language.CPP);

            Assert.That(trackedLines, Is.EqualTo(expectedTrackedLines));

        }

        internal enum ContainingCodeTrackerType
        {
            CoverageLines,
            NotIncluded,
            OtherLines
        }
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        internal class TrackerArgs : IContainingCodeTracker
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public List<ILine> LinesInRange { get; }
            public CodeSpanRange CodeSpanRange { get; }
            public ITextSnapshot Snapshot { get; }
            public SpanTrackingMode SpanTrackingMode { get; }
            public ContainingCodeTrackerType TrackerType { get; }

            public static TrackerArgs ExpectedSingleCoverageLines(ILine line, SpanTrackingMode spanTrackingMode)
            {
                return ExpectedCoverageLines(new List<ILine> { line }, CodeSpanRangeFromLine(line), spanTrackingMode);
            }

            public static TrackerArgs ExpectedCoverageLines(List<ILine> lines, CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(null, lines, codeSpanRange, spanTrackingMode, ContainingCodeTrackerType.CoverageLines);
            }

            public static TrackerArgs ExpectedOtherLines(CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(null, null, codeSpanRange, spanTrackingMode, ContainingCodeTrackerType.OtherLines);
            }

            public static TrackerArgs ExpectedNotIncluded(CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(null, null, codeSpanRange, spanTrackingMode, ContainingCodeTrackerType.NotIncluded);
            }
            public TrackerArgs(
                ITextSnapshot textSnapsot, 
                List<ILine> lines, 
                CodeSpanRange codeSpanRange, 
                SpanTrackingMode spanTrackingMode, 
                ContainingCodeTrackerType trackerType)
            {
                Snapshot = textSnapsot;
                LinesInRange = lines;
                CodeSpanRange = codeSpanRange;
                SpanTrackingMode = spanTrackingMode;
                TrackerType = trackerType;
            }

            private static bool LinesEqual(List<ILine> firstLines, List<ILine> secondLines)
            {
                if (firstLines == null && secondLines == null) return true;
                var linesEqual = firstLines.Count == secondLines.Count;
                if (linesEqual)
                {
                    for (var i = 0; i < firstLines.Count; i++)
                    {
                        if (firstLines[i] != secondLines[i])
                        {
                            linesEqual = false;
                            break;
                        }
                    }
                }
                return linesEqual;
            }

            public override bool Equals(object obj)
            {
                var otherTrackerArgs = obj as TrackerArgs;
                return SpanTrackingMode == otherTrackerArgs.SpanTrackingMode
                    && TrackerType == otherTrackerArgs.TrackerType
                    && CodeSpanRange.Equals(otherTrackerArgs.CodeSpanRange)
                    && LinesEqual(LinesInRange, otherTrackerArgs.LinesInRange);
            }

            public IEnumerable<IDynamicLine> Lines => throw new System.NotImplementedException();

            public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
            {
                throw new System.NotImplementedException();
            }

            public ContainingCodeTrackerState GetState()
            {
                throw new NotImplementedException();
            }
        }

        class DummyCodeSpanRangeContainingCodeTrackerFactory : ICodeSpanRangeContainingCodeTrackerFactory
        {
            public IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(textSnapshot, lines, containingRange, spanTrackingMode, ContainingCodeTrackerType.CoverageLines);
            }

            public IContainingCodeTracker CreateDirty(ITextSnapshot currentSnapshot, CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode)
            {
                throw new NotImplementedException();
            }

            public IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(textSnapshot, null, containingRange, spanTrackingMode, ContainingCodeTrackerType.NotIncluded);
            }

            public IContainingCodeTracker CreateOtherLines(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
            {
                return new TrackerArgs(textSnapshot, null, containingRange, spanTrackingMode, ContainingCodeTrackerType.OtherLines);
            }
        }

        [TestCaseSource(typeof(RoslynDataClass), nameof(RoslynDataClass.TestCases))]
        public void Should_Create_ContainingCodeTrackers_In_Order_Contained_Lines_And_Single_Line_When_Roslyn_Languages
        (
            List<CodeSpanRange> codeSpanRanges,
            List<ILine> lines,
            List<TrackerArgs> expected,
            Action<Mock<ITextSnapshotLineExcluder>,bool> setUpExcluder,
            int lineCount
        )
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<ICodeSpanRangeContainingCodeTrackerFactory>(new DummyCodeSpanRangeContainingCodeTrackerFactory());
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            setUpExcluder(autoMoqer.GetMock<ITextSnapshotLineExcluder>(), isCSharp);

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(lineCount);
            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            var textSpans = codeSpanRanges.Select(codeSpanRange => new TextSpan(codeSpanRange.StartLine, codeSpanRange.EndLine - codeSpanRange.StartLine)).ToList();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object)).ReturnsAsync(textSpans);
            textSpans.ForEach(textSpan =>
            {
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.Start)).Returns(textSpan.Start);
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(textSpan.End)).Returns(textSpan.End);
            });

            var newCodeTracker = autoMoqer.GetMock<INewCodeTracker>().Object;
            autoMoqer.Setup<INewCodeTrackerFactory, INewCodeTracker>(newCodeTrackerFactory => newCodeTrackerFactory.Create(isCSharp)).Returns(newCodeTracker);

            var expectedTrackedLines = new TrackedLines(null, null);
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            mockContainingCodeTrackedLinesFactory.Setup(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(It.IsAny<List<IContainingCodeTracker>>(), newCodeTracker)
            ).Callback<List<IContainingCodeTracker>, INewCodeTracker>((containingCodeTrackers, _) =>
                {
                    var invocationArgs = containingCodeTrackers.Select(t => t as TrackerArgs).ToList();
                    Assert.True(invocationArgs.Select(args => args.Snapshot).All(snapshot => snapshot == mockTextSnapshot.Object));
                    Assert.That(invocationArgs, Is.EqualTo(expected));
                }).Returns(expectedTrackedLines);

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, mockTextSnapshot.Object, isCSharp ? Language.CSharp : Language.VB);

            Assert.That(trackedLines, Is.SameAs(expectedTrackedLines));

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
                    Action<Mock<ITextSnapshotLineExcluder>,bool> setUpCodeExcluder,
                    int lineCount = 100,
                    string testName = null

                ) : base(codeSpanRanges, lines, expected, setUpCodeExcluder,lineCount)
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
            
            private static void ExcludeAllLines(Mock<ITextSnapshotLineExcluder> mockCodeLineExcluder,bool isCSharp)
            {
                mockCodeLineExcluder.Setup(excluder => excluder.ExcludeIfNotCode(It.IsAny<ITextSnapshot>(), It.IsAny<int>(), isCSharp)).Returns(true);
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
                            TrackerArgs.ExpectedCoverageLines(new List<ILine>{ test1Lines[0], test1Lines[1] }, test1CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedCoverageLines(new List<ILine>{ test1Lines[2], test1Lines[3] }, test1CodeSpanRanges[1], SpanTrackingMode.EdgeExclusive)
                            },
                            ExcludeAllLines
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
                            TrackerArgs.ExpectedSingleCoverageLines(test2Lines[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingleCoverageLines(test2Lines[1], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedCoverageLines(new List<ILine>{ test2Lines[2] }, test2CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedNotIncluded(test2CodeSpanRanges[1], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingleCoverageLines(test2Lines[3], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedCoverageLines(new List < ILine > { test2Lines[4] }, test2CodeSpanRanges[2], SpanTrackingMode.EdgeExclusive),
                        }, ExcludeAllLines);
                    }

                    {
                        var test3CodeSpanRanges = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(10,20),
                        };
                        var test3Lines = new List<ILine> { GetLine(21) }; // for line number adjustment
                        yield return new RoslynTestCase(test3CodeSpanRanges, test3Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedCoverageLines(test3Lines, test3CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive)
                        }, ExcludeAllLines);
                    }

                    {
                        var test4CodeSpanRanges = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(10,20),
                        };
                        var test4Lines = new List<ILine> { GetLine(50) };
                        yield return new RoslynTestCase(test4CodeSpanRanges, test4Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedNotIncluded(test4CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedSingleCoverageLines(test4Lines[0], SpanTrackingMode.EdgeExclusive)
                        }, ExcludeAllLines);
                    }

                    {
                        void ExcludeOrIncludeCodeLines(Mock<ITextSnapshotLineExcluder> mockTextSnapshotLineExcluder, bool isCSharp, List<int> codeLineNumbers, bool exclude)
                        {
                            mockTextSnapshotLineExcluder.Setup(excluder => excluder.ExcludeIfNotCode(
                                It.IsAny<ITextSnapshot>(),
                                It.Is<int>(lineNumber => codeLineNumbers.Contains(lineNumber)), isCSharp)).Returns(exclude);
                        }
                        void SetupExcluder(Mock<ITextSnapshotLineExcluder> mockTextSnapshotLineExcluder, bool isCSharp)
                        {
                            ExcludeOrIncludeCodeLines(mockTextSnapshotLineExcluder, isCSharp, new List<int> { 0, 2, 21, 23 }, false);
                            ExcludeOrIncludeCodeLines(mockTextSnapshotLineExcluder, isCSharp, new List<int> { 1, 3, 4, 22 }, true);
                        }
                        var test5CodeSpanRanges = new List<CodeSpanRange>
                        {
                            new CodeSpanRange(5,20),
                        };
                        var test5Lines = new List<ILine> { GetLine(15) };
                        yield return new RoslynTestCase(test5CodeSpanRanges, test5Lines, new List<TrackerArgs>
                        {
                            TrackerArgs.ExpectedOtherLines(new CodeSpanRange(0,0), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedOtherLines(new CodeSpanRange(2,2), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedCoverageLines(test5Lines, test5CodeSpanRanges[0], SpanTrackingMode.EdgeExclusive),
                            TrackerArgs.ExpectedOtherLines(new CodeSpanRange(21,21), SpanTrackingMode.EdgeNegative),
                            TrackerArgs.ExpectedOtherLines( new CodeSpanRange(23,23), SpanTrackingMode.EdgeNegative),
                        }, SetupExcluder, 24, "Other lines");
                    }

                }
            }
        }
    }
}
