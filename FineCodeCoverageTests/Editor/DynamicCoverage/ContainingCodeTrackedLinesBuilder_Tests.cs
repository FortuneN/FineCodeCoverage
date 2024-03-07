using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    class Line : ILine
    {
        public Line(int number, CoverageType coverageType)
        {
            Number = number;
            CoverageType = coverageType;
        }
        public override bool Equals(object obj)
        {
            var other = obj as ILine;
            return other.Number == Number && other.CoverageType == CoverageType;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = 1698846147;
            hashCode = hashCode * -1521134295 + Number.GetHashCode();
            hashCode = hashCode * -1521134295 + CoverageType.GetHashCode();
            return hashCode;
        }

        public int Number { get; }

        public CoverageType CoverageType { get; }
    }

    internal static class TestHelper
    {
        public static CodeSpanRange CodeSpanRangeFromLine(ILine line)
        {
            return CodeSpanRange.SingleLine(line.Number - 1);
        }
    }
    internal class ContainingCodeTrackedLinesBuilder_CPP_Tests
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
            var firstLine = lines[0];
            var firstCodeSpanRange = TestHelper.CodeSpanRangeFromLine(firstLine);
            var secondLine = lines[1];
            var secondCodeSpanRange = TestHelper.CodeSpanRangeFromLine(secondLine);
            var mockContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
                containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { firstLine }, firstCodeSpanRange, SpanTrackingMode.EdgeExclusive)
            ).Returns(containingCodeTrackers[0]);
            mockContainingCodeTrackerFactory.Setup(containingCodeTrackerFactory =>
               containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { secondLine }, secondCodeSpanRange, SpanTrackingMode.EdgeExclusive)
           ).Returns(containingCodeTrackers[1]);

            var expectedTrackedLines = new TrackedLines(null, null, null);
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(containingCodeTrackers, null, null)
                       ).Returns(expectedTrackedLines);

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, textSnapshot, Language.CPP);

            Assert.That(trackedLines, Is.EqualTo(expectedTrackedLines));

        }

        [Test]
        public void Should_Use_CPP_Deserialized_When_CodeSpanRange_Within_Total_Lines()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(40);
            var autoMoqer = new AutoMoqer();
            var mockCodeSpanRangeContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            var coverageLineTracker = new Mock<IContainingCodeTracker>().Object;
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                    mockTextSnapshot.Object,
                    new List<ILine> { new Line(1, CoverageType.Covered) },
                    new CodeSpanRange(10, 20),
                    SpanTrackingMode.EdgeExclusive
                    )
            ).Returns(coverageLineTracker);
            var dirtyLineTracker = new Mock<IContainingCodeTracker>().Object;
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateDirty(
                    mockTextSnapshot.Object,
                    new CodeSpanRange(25, 30),
                    SpanTrackingMode.EdgeExclusive
                    )
            ).Returns(dirtyLineTracker);
            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            var serializedState = new SerializedState(new CodeSpanRange(10, 20), ContainingCodeTrackerType.CoverageLines, new List<DynamicLine>
            {
                new DynamicLine(0, DynamicCoverageType.Covered)
            });
            var serializedState2 = new SerializedState(new CodeSpanRange(25, 30), ContainingCodeTrackerType.CoverageLines, new List<DynamicLine>
            {
                new DynamicLine(3, DynamicCoverageType.Dirty)
            });
            var serializedState3 = new SerializedState(new CodeSpanRange(50, 60), ContainingCodeTrackerType.CoverageLines, new List<DynamicLine>());
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns(new List<SerializedState> { serializedState, serializedState2, serializedState3 });

            var expectedTrackedLines = new TrackedLines(null, null, null);
            autoMoqer.Setup<IContainingCodeTrackedLinesFactory, TrackedLines>(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    new List<IContainingCodeTracker> { coverageLineTracker, dirtyLineTracker },
                    null,
                    null
                )).Returns(expectedTrackedLines);


            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var trackedLines = containingCodeTrackedLinesBuilder.Create("serializedState", mockTextSnapshot.Object, Language.CPP);

            Assert.That(expectedTrackedLines, Is.SameAs(trackedLines));
        }
    }

    [TestFixture(true)]
    [TestFixture(false)]
    internal class ContainingCodeTrackedLinesBuilder_Tests
    {
        private readonly bool isCSharp;

        public ContainingCodeTrackedLinesBuilder_Tests(bool isCSharp)
        {
            this.isCSharp = isCSharp;
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
                return ExpectedCoverageLines(new List<ILine> { line }, TestHelper.CodeSpanRangeFromLine(line), spanTrackingMode);
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
            new List<bool> { true, false }.ForEach(UseRoslynWhenTextChanges =>
            {
                var autoMoqer = new AutoMoqer();
                var mockAppOptions = new Mock<IAppOptions>();
                mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode)
                    .Returns(UseRoslynWhenTextChanges ? EditorCoverageColouringMode.UseRoslynWhenTextChanges : EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges);
                autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(appOptionsProvider => appOptionsProvider.Get())
                    .Returns(mockAppOptions.Object);
                autoMoqer.SetInstance<ICodeSpanRangeContainingCodeTrackerFactory>(new DummyCodeSpanRangeContainingCodeTrackerFactory());
                autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
                var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
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
                autoMoqer.Setup<INewCodeTrackerFactory, INewCodeTracker>(newCodeTrackerFactory => newCodeTrackerFactory.Create(isCSharp))
                    .Returns(newCodeTracker);

                var expectedTrackedLines = new TrackedLines(null, null, null);
                var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
                mockContainingCodeTrackedLinesFactory.Setup(
                    containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                        It.IsAny<List<IContainingCodeTracker>>(), 
                        newCodeTracker, 
                        UseRoslynWhenTextChanges ? containingCodeTrackedLinesBuilder : null)
                ).Callback<List<IContainingCodeTracker>, INewCodeTracker, IFileCodeSpanRangeService>((containingCodeTrackers, _, __) =>
                {
                    var invocationArgs = containingCodeTrackers.Select(t => t as TrackerArgs).ToList();
                    Assert.True(invocationArgs.Select(args => args.Snapshot).All(snapshot => snapshot == mockTextSnapshot.Object));
                    Assert.That(invocationArgs, Is.EqualTo(expected));
                }).Returns(expectedTrackedLines);


                var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, mockTextSnapshot.Object, isCSharp ? Language.CSharp : Language.VB);

                Assert.That(trackedLines, Is.SameAs(expectedTrackedLines));
            });
            

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

        [TestCase(ContainingCodeTrackerType.CoverageLines, 1, DynamicCoverageType.Covered)]
        [TestCase(ContainingCodeTrackerType.NotIncluded, 1, DynamicCoverageType.NotIncluded)]
        public void Should_Serialize_State_From_TrackedLines_ContainingCodeTrackers(
            ContainingCodeTrackerType containingCodeTrackerType, int lineNumber, DynamicCoverageType coverageType
        )
        {
            var autoMoqer = new AutoMoqer();

            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.SerializeObject(It.IsAny<object>())).Returns("SerializedState");
            
            var mockContainingCodeTracker = new Mock<IContainingCodeTracker>();
            var codeSpanRange = new CodeSpanRange(1, 2);
            var containingCodeTrackerState = new ContainingCodeTrackerState(containingCodeTrackerType, codeSpanRange, new List<IDynamicLine> { new DynamicLine(lineNumber,coverageType) });
            mockContainingCodeTracker.Setup(containingCodeTracker => containingCodeTracker.GetState()).Returns(containingCodeTrackerState);
            var containingCodeTrackers = new List<IContainingCodeTracker> { 
                mockContainingCodeTracker.Object,
            };

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var serialized = containingCodeTrackedLinesBuilder.Serialize(
                new TrackedLines(containingCodeTrackers, null, null));
            
            Assert.That("SerializedState", Is.EqualTo(serialized));

            var serializedState = mockJsonConvertService.Invocations.GetMethodInvocationSingleArgument<List<SerializedState>>(
                nameof(IJsonConvertService.SerializeObject)).Single().Single();
            
            Assert.That(serializedState.Type, Is.EqualTo(containingCodeTrackerType));
            Assert.That(serializedState.CodeSpanRange, Is.SameAs(codeSpanRange));
            var serializedLine = serializedState.Lines.Single();
            Assert.That(serializedLine.Number, Is.EqualTo(lineNumber));
            Assert.That(serializedLine.CoverageType, Is.EqualTo(coverageType));
        }

        private Mock<IAppOptions> EnsureAppOptions(AutoMoqer autoMoqer)
        {
            var mockAppOptions = new Mock<IAppOptions>();
            autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(
                appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);
            return mockAppOptions;
        }

        private void Should_Use_Deserialized_IContainingCodeTracker_If_CodeSpanRange_Has_Not_Changed(
            ContainingCodeTrackerType containingCodeTrackerType,
            List<DynamicLine> dynamicLines,
            Action<
                Mock<ICodeSpanRangeContainingCodeTrackerFactory>, 
                IContainingCodeTracker,
                CodeSpanRange,
                ITextSnapshot> setupContainingCodeTrackerFactory
            )
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(1)).Returns(10);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(3)).Returns(20);

            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            EnsureAppOptions(autoMoqer);

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            var codeSpanRange = new CodeSpanRange(10, 20);
            var serializedState = new SerializedState(codeSpanRange, containingCodeTrackerType, dynamicLines);
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns(new List<SerializedState> { serializedState });

            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object))
                .ReturnsAsync(new List<TextSpan> { new TextSpan(1, 2) });

            var newCodeTracker = new Mock<INewCodeTracker>().Object;
            autoMoqer.Setup<INewCodeTrackerFactory, INewCodeTracker>(
                newCodeTrackerFactory => newCodeTrackerFactory.Create(
                    isCSharp,
                    new List<int> { },
                    mockTextSnapshot.Object)).Returns(newCodeTracker);

            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            setupContainingCodeTrackerFactory(
                autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>(),
                containingCodeTracker,
                codeSpanRange,
                mockTextSnapshot.Object);

            var expectedTrackedLines = new TrackedLines(null, null, null);
            autoMoqer.Setup<IContainingCodeTrackedLinesFactory, TrackedLines>(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    new List<IContainingCodeTracker> { containingCodeTracker },
                   newCodeTracker,
                   containingCodeTrackedLinesBuilder
                )).Returns(expectedTrackedLines);

            var trackedLines = containingCodeTrackedLinesBuilder.Create(
                "serializedState", 
                mockTextSnapshot.Object, 
                isCSharp ? Language.CSharp : Language.VB
            );
            Assert.That(expectedTrackedLines, Is.SameAs(trackedLines));
        }

        [Test]
        public void Should_Use_Deserialized_OtherLinesTracker_If_CodeSpanRange_Has_Not_Changed()
        {
            Should_Use_Deserialized_IContainingCodeTracker_If_CodeSpanRange_Has_Not_Changed(
                ContainingCodeTrackerType.OtherLines,
                new List<DynamicLine> { },
                (mockContainingCodeTrackerFactory, containingCodeTracker, codeSpanRange, textSnapshot) =>
                {
                    mockContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateOtherLines(
                            textSnapshot,
                            codeSpanRange,
                            SpanTrackingMode.EdgeNegative
                        )).Returns(containingCodeTracker);
                }
            );
        }

        [Test]
        public void Should_Use_Deserialized_NotIncludedTracker_If_CodeSpanRange_Has_Not_Changed()
        {
            Should_Use_Deserialized_IContainingCodeTracker_If_CodeSpanRange_Has_Not_Changed(
                ContainingCodeTrackerType.NotIncluded,
                new List<DynamicLine> { },
                (mockContainingCodeTrackerFactory, containingCodeTracker, codeSpanRange, textSnapshot) =>
                {
                    mockContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateNotIncluded(
                            textSnapshot,
                            codeSpanRange,
                            SpanTrackingMode.EdgeExclusive
                        )).Returns(containingCodeTracker);
                }
            );
        }

        
        [TestCase(DynamicCoverageType.Covered, CoverageType.Covered)]
        [TestCase(DynamicCoverageType.NotCovered, CoverageType.NotCovered)]
        [TestCase(DynamicCoverageType.Partial, CoverageType.Partial)]
        public void Should_Use_Deserialized_CoverageLinesTracker_If_CodeSpanRange_Has_Not_Changed(
            DynamicCoverageType dynamicCoverageType,
            CoverageType expectedCoverageType)
        {
            Should_Use_Deserialized_IContainingCodeTracker_If_CodeSpanRange_Has_Not_Changed(
                ContainingCodeTrackerType.CoverageLines,
                new List<DynamicLine> { new DynamicLine(1, dynamicCoverageType), new DynamicLine(2, dynamicCoverageType)},
                (mockContainingCodeTrackerFactory, containingCodeTracker, codeSpanRange, textSnapshot) =>
                {
                    mockContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshot,
                            new List<ILine> { new Line(2, expectedCoverageType), new Line(3, expectedCoverageType) },
                            codeSpanRange,
                            SpanTrackingMode.EdgeExclusive
                        )).Returns(containingCodeTracker);
                }
            );
        }

        [Test]
        public void Should_Use_Deserialized_CoverageLinesTracker_For_Dirty_When_DirtyIf_CodeSpanRange_Has_Not_Changed()
        {
            Should_Use_Deserialized_IContainingCodeTracker_If_CodeSpanRange_Has_Not_Changed(
                ContainingCodeTrackerType.CoverageLines,
                new List<DynamicLine> { new DynamicLine(1, DynamicCoverageType.Dirty) },
                (mockContainingCodeTrackerFactory, containingCodeTracker, codeSpanRange, textSnapshot) =>
                {
                    mockContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateDirty(
                            textSnapshot,
                            codeSpanRange,
                            SpanTrackingMode.EdgeExclusive
                        )).Returns(containingCodeTracker);
                }
            );
        }

        [Test]
        public void Should_Not_Use_Deserialized_If_CodeSpanRange_Has_Changed()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(1)).Returns(10);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(3)).Returns(20);
            
            var autoMoqer = new AutoMoqer();
            EnsureAppOptions(autoMoqer);
           
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            var codeSpanRange = new CodeSpanRange(100, 200);
            var serializedState = new SerializedState(codeSpanRange, ContainingCodeTrackerType.OtherLines, new List<DynamicLine>());
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns(new List<SerializedState> { serializedState });

            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object))
                .ReturnsAsync(new List<TextSpan> { new TextSpan(1, 2) });

            var expectedTrackedLines = new TrackedLines(null, null, null);
            autoMoqer.Setup<IContainingCodeTrackedLinesFactory, TrackedLines>(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    new List<IContainingCodeTracker> { },
                    It.IsAny<INewCodeTracker>(),
                    It.IsAny<IFileCodeSpanRangeService>()
                )).Returns(expectedTrackedLines);

            var trackedLines = containingCodeTrackedLinesBuilder.Create(
                "serializedState", 
                mockTextSnapshot.Object, 
                isCSharp ? Language.CSharp : Language.VB
            );
            
            Assert.That(expectedTrackedLines, Is.SameAs(trackedLines));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Deserialize_Dependent_Upon_AppOption_EditorCoverageColouringMode_UseRoslynWhenTextChanges(bool useRoslynWhenTextChanges)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(1)).Returns(10);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(3)).Returns(20);

            var autoMoqer = new AutoMoqer();
            var mockAppOptions = EnsureAppOptions(autoMoqer);
            mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode)
                .Returns(useRoslynWhenTextChanges ? EditorCoverageColouringMode.UseRoslynWhenTextChanges : EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges);

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            var codeSpanRange = new CodeSpanRange(100, 200);
            var serializedState = new SerializedState(codeSpanRange, ContainingCodeTrackerType.OtherLines, new List<DynamicLine>());
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns(new List<SerializedState> { serializedState });

            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object))
                .ReturnsAsync(new List<TextSpan> { new TextSpan(1, 2) });

            var newCodeTracker = new Mock<INewCodeTracker>().Object;
            var expectedLines = useRoslynWhenTextChanges ? new List<int> { 10 } : new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            autoMoqer.Setup<INewCodeTrackerFactory, INewCodeTracker>(
                newCodeTrackerFactory => newCodeTrackerFactory.Create(
                    isCSharp,
                    expectedLines,
                    mockTextSnapshot.Object)).Returns(newCodeTracker);

            var expectedTrackedLines = new TrackedLines(null, null, null);
            autoMoqer.Setup<IContainingCodeTrackedLinesFactory, TrackedLines>(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    new List<IContainingCodeTracker> { },
                    newCodeTracker,
                    useRoslynWhenTextChanges ? containingCodeTrackedLinesBuilder : null
                )).Returns(expectedTrackedLines);

            var trackedLines = containingCodeTrackedLinesBuilder.Create(
                "serializedState",
                mockTextSnapshot.Object,
                isCSharp ? Language.CSharp : Language.VB
            );

            Assert.That(expectedTrackedLines, Is.SameAs(trackedLines));
        }

        [Test]
        public void Should_IFileCodeSpanRangeService_Using_Roslyn_Distinct()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(1)).Returns(1);
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(11)).Returns(1);
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(15)).Returns(1);
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(20)).Returns(1);
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(30)).Returns(2);
            mockTextSnapshot.Setup(textSnaphot => textSnaphot.GetLineNumberFromPosition(40)).Returns(3);

            var autoMoqer = new AutoMoqer();
            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object))
                    .ReturnsAsync(new List<TextSpan> { new TextSpan(1, 10), new TextSpan(15,5),new TextSpan(30,10) });

            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var fileCodeSpanRanges = containingCodeTrackedLinesBuilder.GetFileCodeSpanRanges(mockTextSnapshot.Object);
            
            Assert.That(fileCodeSpanRanges, Is.EqualTo(new List<CodeSpanRange> { CodeSpanRange.SingleLine(1), new CodeSpanRange(2, 3) }));
        }
    }
}
