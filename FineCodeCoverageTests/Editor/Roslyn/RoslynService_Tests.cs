using AutoMoq;
using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverageTests.Editor.Roslyn
{
    internal class CSharpContainingCodeVisitor_Tests
    {
        [Test]
        public void Should_Visit_Methods()
        {
            var cSharpContainingCodeVisitor = new CSharpContainingCodeVisitor();

            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public void MyMethod()
        {
            var x = 1;
        }
    }
}
";
            var rootNode = SyntaxFactory.ParseCompilationUnit(text);
            var textSpans = cSharpContainingCodeVisitor.GetSpans(rootNode);
            Assert.That(textSpans, Has.Count.EqualTo(1));

        }
    }
    internal class RoslynService_Tests
    {
        [Test]
        public async Task Should_Return_Empty_TextSpans_When_RootNodeAndLanguage_Is_Null_Async()
        {
            var autoMoqer = new AutoMoqer();
            var roslynService = autoMoqer.Create<RoslynService>();

            var containingCodeSpans = await roslynService.GetContainingCodeSpansAsync(new Mock<ITextSnapshot>().Object);

            Assert.That(containingCodeSpans, Is.Empty);
        }

        [TestCase(LanguageNames.CSharp,true)]
        [TestCase(LanguageNames.VisualBasic, false)]
        public async Task Should_Return_TextSpans_For_The_Language_Async(string language,bool languageIsCSharp)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var rootNode = SyntaxFactory.ParseCompilationUnit("");

            var autoMoqer = new AutoMoqer();
            var roslynService = autoMoqer.Create<RoslynService>();
            var mockLanguageContainingCodeVisitorFactory = autoMoqer.GetMock<ILanguageContainingCodeVisitorFactory>();
            var mockLanguageContainingCodeVisitor = new Mock<ILanguageContainingCodeVisitor>();
            var textSpans = new List<TextSpan> { new TextSpan(0, 1) };
            mockLanguageContainingCodeVisitor.Setup(languageContainingCodeVisitor => languageContainingCodeVisitor.GetSpans(rootNode)).Returns(textSpans);
            mockLanguageContainingCodeVisitorFactory.Setup(x => x.Create(languageIsCSharp)).Returns(mockLanguageContainingCodeVisitor.Object);

            var mockTextSnapshotToSyntaxService = autoMoqer.GetMock<ITextSnapshotToSyntaxService>();
            
            mockTextSnapshotToSyntaxService.Setup(x => x.GetRootAndLanguageAsync(textSnapshot)).ReturnsAsync(new RootNodeAndLanguage(rootNode, language));
            var containingCodeSpans = await roslynService.GetContainingCodeSpansAsync(textSnapshot);

            Assert.That(containingCodeSpans, Is.SameAs(textSpans));
        }
    }

}
