using AutoMoq;
using FineCodeCoverage.Editor.Roslyn;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.Roslyn
{
    internal class LanguageContainingCodeVisitorFactory_Tests
    {
        [Test]
        public void Should_Return_CSharp_ContainingCodeVisitor_When_IsCSharp_Is_True()
        {
            var autoMoqer = new AutoMoqer();
            var languageContainingCodeVisitorFactory = autoMoqer.Create<LanguageContainingCodeVisitorFactory>();

            var languageContainingCodeVisitor = languageContainingCodeVisitorFactory.Create(true);

            Assert.That(languageContainingCodeVisitor, Is.InstanceOf<CSharpContainingCodeVisitor>());
        }

        [Test]
        public void Should_Return_VisualBasic_ContainingCodeVisitor_When_IsCSharp_Is_False()
        {
            var autoMoqer = new AutoMoqer();
            var languageContainingCodeVisitorFactory = autoMoqer.Create<LanguageContainingCodeVisitorFactory>();

            var languageContainingCodeVisitor = languageContainingCodeVisitorFactory.Create(false);

            Assert.That(languageContainingCodeVisitor, Is.InstanceOf<VBContainingCodeVisitor>());
        }
    }

}
