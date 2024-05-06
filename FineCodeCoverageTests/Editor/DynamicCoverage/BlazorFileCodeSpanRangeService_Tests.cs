using System.Collections.Generic;
using System.Linq;
using AutoMoq;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using FineCodeCoverage.Editor.DynamicCoverage.Utilities;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class BlazorFileCodeSpanRangeService_Tests
    {
        [Test]
        public void Should_Return_Null_If_Cannot_Find_Syntax_Root_Of_Generated_Document()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            var mockTextBuffer = new Mock<ITextBuffer>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.TextBuffer).Returns(mockTextBuffer.Object);

            var autoMoqer = new AutoMoqer();
            var razorGeneratedFilePathMatcher = autoMoqer.GetMock<IBlazorGeneratedFilePathMatcher>().Object;
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            autoMoqer.GetMock<ITextInfoFactory>().Setup(t => t.GetFilePath(mockTextBuffer.Object)).Returns("path");

            var mockRazorGeneratedDocumentRootFinder = autoMoqer.GetMock<IBlazorGeneratedDocumentRootFinder>();
            mockRazorGeneratedDocumentRootFinder.Setup(
                razorGeneratedDocumentootFinder => razorGeneratedDocumentootFinder.FindSyntaxRootAsync(mockTextBuffer.Object, "path", razorGeneratedFilePathMatcher)
            ).ReturnsAsync((SyntaxNode)null);

            var fileCodeSpanRanges = autoMoqer.Create<BlazorFileCodeSpanRangeService>().GetFileCodeSpanRanges(mockTextSnapshot.Object);

            Assert.IsNull(fileCodeSpanRanges);
            mockRazorGeneratedDocumentRootFinder.VerifyAll();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Use_The_Generated_Coverage_Syntax_Nodes_Mapped_To_Razor_File_For_The_CodeSpanRange(
            bool firstMapsBack    
        )
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            var mockTextBuffer = new Mock<ITextBuffer>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.TextBuffer).Returns(mockTextBuffer.Object);

            var autoMoqer = new AutoMoqer();
            var razorGeneratedFilePathMatcher = autoMoqer.GetMock<IBlazorGeneratedFilePathMatcher>().Object;
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());
            autoMoqer.GetMock<ITextInfoFactory>().Setup(t => t.GetFilePath(mockTextBuffer.Object)).Returns("path");

            var mockRazorGeneratedDocumentRootFinder = autoMoqer.GetMock<IBlazorGeneratedDocumentRootFinder>();
            SyntaxNode rootSyntaxNode = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            SyntaxNode codeCoverageNode1 = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            SyntaxNode codeCoverageNode2 = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            mockRazorGeneratedDocumentRootFinder.Setup(
                razorGeneratedDocumentootFinder => razorGeneratedDocumentootFinder.FindSyntaxRootAsync(mockTextBuffer.Object, "path", razorGeneratedFilePathMatcher)
            ).ReturnsAsync(rootSyntaxNode);

            var mockCSharpCodeCoverageNodeVisitor = autoMoqer.GetMock<ICSharpCodeCoverageNodeVisitor>();
            mockCSharpCodeCoverageNodeVisitor.Setup(cSharpCodeCoverageNodeVisitor => cSharpCodeCoverageNodeVisitor.GetNodes(rootSyntaxNode))
                .Returns(new List<SyntaxNode> { codeCoverageNode1, codeCoverageNode2 });
            var mockSyntaxNodeLocationMapper = autoMoqer.GetMock<ISyntaxNodeLocationMapper>();

            var linePositionSpan1 = new LinePositionSpan(new LinePosition(1, 1), new LinePosition(2, 1));
            var fileLinePositionSpan1 = new FileLinePositionSpan(firstMapsBack ? "path" : "",linePositionSpan1);
            var linePositionSpan2 = new LinePositionSpan(new LinePosition(3, 1), new LinePosition(4, 1));
            var fileLinePositionSpan2 = new FileLinePositionSpan(firstMapsBack ? "" : "path", linePositionSpan2);
            var expectedCodeSpanRange = firstMapsBack ? new CodeSpanRange(1, 2) : new CodeSpanRange(3, 4);

            mockSyntaxNodeLocationMapper.Setup(syntaxNodeLocationMapper => syntaxNodeLocationMapper.Map(codeCoverageNode1))
                .Returns(fileLinePositionSpan1);
            mockSyntaxNodeLocationMapper.Setup(syntaxNodeLocationMapper => syntaxNodeLocationMapper.Map(codeCoverageNode2))
                .Returns(fileLinePositionSpan2);


            var fileCodeSpanRanges = autoMoqer.Create<BlazorFileCodeSpanRangeService>().GetFileCodeSpanRanges(mockTextSnapshot.Object);
            var fileCodeSpanRange = fileCodeSpanRanges.Single();

            Assert.That(expectedCodeSpanRange, Is.EqualTo(fileCodeSpanRange));
        }
    }
}
