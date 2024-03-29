﻿using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.Tagging.Base
{
    internal class LineSpanLogic_Tests
    {
        class Line : ILine
        {
            public int Number { get; set; }

            public CoverageType CoverageType
            {
                get; set;
            }
            ITextSnapshotLine GetMockedLine(ITextSnapshot textSnapshot, int lineNumber, int start = 0, int end = 0)
            {
                var mockTextSnapshotLine = new Mock<ITextSnapshotLine>();
                mockTextSnapshotLine.SetupGet(textSnapshotLine => textSnapshotLine.LineNumber).Returns(lineNumber);
                mockTextSnapshotLine.SetupGet(textSnapshotLine => textSnapshotLine.Start).Returns(new SnapshotPoint(textSnapshot, start));
                mockTextSnapshotLine.SetupGet(textSnapshotLine => textSnapshotLine.End).Returns(new SnapshotPoint(textSnapshot, end));
                return mockTextSnapshotLine.Object;
            }

            [Test]
            public void Should_ForEach_Normalized_Span_Should_Have_A_Full_LineSpan_For_Each_Coverage_Line_In_The_Range()
            {
                var mockBufferLineCoverage = new Mock<IBufferLineCoverage>();
                var firstLine = CreateDynamicLine(5, DynamicCoverageType.Covered);
                var secondLine = CreateDynamicLine(17, DynamicCoverageType.NotCovered);
                IDynamicLine CreateDynamicLine(int lineNumber, DynamicCoverageType coverageType)
                {
                    var mockDynamicLine = new Mock<IDynamicLine>();
                    mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(lineNumber);
                    mockDynamicLine.SetupGet(dynamicLine => dynamicLine.CoverageType).Returns(coverageType);
                    return mockDynamicLine.Object;
                }
                mockBufferLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines(0, 9)).Returns(new List<IDynamicLine>
                {
                    firstLine
                });
                mockBufferLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines(14, 19)).Returns(new List<IDynamicLine>
                {
                    secondLine
                });

                var mockTextSnapshot = new Mock<ITextSnapshot>();
                mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(300);
                var txtSnapshot = mockTextSnapshot.Object;

                // GetContainingLine() comes from ITextSnapshot.GetLineFromPosition
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(1)).Returns(GetMockedLine(txtSnapshot, 0));
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(199)).Returns(GetMockedLine(txtSnapshot, 9));

                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(200)).Returns(GetMockedLine(txtSnapshot, 14));
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(299)).Returns(GetMockedLine(txtSnapshot, 19));


                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromLineNumber(5)).Returns(GetMockedLine(txtSnapshot, 5, 50, 60));
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromLineNumber(17)).Returns(GetMockedLine(txtSnapshot, 17, 250, 260));

                // is a normalized span collection linked to the ITextSnapshot
                var normalizedSpanCollection = new NormalizedSnapshotSpanCollection(mockTextSnapshot.Object, new List<Span> {
                    Span.FromBounds(1, 199),
                    Span.FromBounds(200, 299)
                });

                var lineSpanLogic = new LineSpanLogic();
                var lineSpans = lineSpanLogic.GetLineSpans(mockBufferLineCoverage.Object, normalizedSpanCollection).ToList();

                Assert.That(lineSpans.Count, Is.EqualTo(2));
                Assert.That(lineSpans[0].Line, Is.SameAs(firstLine));
                Assert.That(lineSpans[0].Span, Is.EqualTo(new SnapshotSpan(txtSnapshot, new Span(50, 10))));
                Assert.That(lineSpans[1].Line, Is.SameAs(secondLine));
                Assert.That(lineSpans[1].Span, Is.EqualTo(new SnapshotSpan(txtSnapshot, new Span(250, 10))));
                
            }

        }

    }
}