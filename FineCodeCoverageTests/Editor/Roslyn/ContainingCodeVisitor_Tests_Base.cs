using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FineCodeCoverageTests.Editor.Roslyn
{
    internal abstract class ContainingCodeVisitor_Tests_Base
    {
        protected abstract ILanguageContainingCodeVisitor GetVisitor();
        protected abstract SyntaxNode ParseCompilation(string compilationText);

        protected (List<TextSpan>, SyntaxNode) Visit(string compilationText)
        {
           var rootNode = ParseCompilation(compilationText);
            var textSpans = GetVisitor().GetSpans(rootNode);
            return (textSpans, rootNode);
        }

        protected void AssertShouldNotVisit(string compilationText)
        {
            var (textSpans, _) = Visit(compilationText);

            Assert.That(textSpans, Is.Empty);
        }

        protected void AssertTextSpan<T>(SyntaxNode rootNode,TextSpan textSpan) where T : SyntaxNode
        {
            var textSpanNode = rootNode.FindNode(textSpan);
            Assert.That(textSpanNode, Is.TypeOf<T>());
            Assert.That(textSpanNode.GetLeadingNoTrailingSpan(), Is.EqualTo(textSpan));
        }

        protected void AssertShouldVisit<T>(string compilationText) where T : SyntaxNode
        {
            var (textSpans, rootNode) = Visit(compilationText);
            Assert.That(textSpans, Has.Count.EqualTo(1));
            var textSpan = textSpans[0];
            AssertTextSpan<T>(rootNode, textSpan);
        }

    }

}
