using FineCodeCoverage.Editor.Tagging.GlyphMargin;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FineCodeCoverageTests.Editor.Tagging.GlyphMargin
{
    internal class CoverageLineGlyphFactory_Tests
    {
        [Test]
        public void Should_Return_Null_If_GlyphTag_Is_Not_CoverageLineGlyphTag()
        {
            var coverageLineGlyphFactory = new CoverageLineGlyphFactory();
            var result = coverageLineGlyphFactory.GenerateGlyph(new Mock<IWpfTextViewLine>().Object, new Mock<IGlyphTag>().Object);
            Assert.IsNull(result);
        }

        [Apartment(ApartmentState.STA)]
        [Test]
        public void Should_Return_A_Solid_Colour_Rectangle_If_GlyphTag_Is_CoverageLineGlyphTag()
        {
            var coverageLineGlyphFactory = new CoverageLineGlyphFactory();
            var result = coverageLineGlyphFactory.GenerateGlyph(new Mock<IWpfTextViewLine>().Object, new CoverageLineGlyphTag(Colors.DeepPink));

            var rectangle = result as Rectangle;
            var fill = rectangle.Fill as SolidColorBrush;
            Assert.That(fill.Color, Is.EqualTo(Colors.DeepPink));
        }
    }
}