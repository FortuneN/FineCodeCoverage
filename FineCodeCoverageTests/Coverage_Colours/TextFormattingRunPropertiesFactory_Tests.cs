using AutoMoq;
using FineCodeCoverage.Impl;
using NUnit.Framework;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    public class TextFormattingRunPropertiesFactory_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Bold_If_Bold(bool bold)
        {
            var autoMoqer = new AutoMoqer();
            var textFormattingRunPropertiesFactory = autoMoqer.Create<TextFormattingRunPropertiesFactory>();
            Assert.That(textFormattingRunPropertiesFactory.Create(FontAndColorsInfoFactory.CreateFontAndColorsInfo(bold)).Bold,Is.EqualTo(bold));
        }

        [Test]
        public void Should_Set_The_Foreground()
        {
            var autoMoqer = new AutoMoqer();
            var textFormattingRunPropertiesFactory = autoMoqer.Create<TextFormattingRunPropertiesFactory>();
            var foegroundBrush = textFormattingRunPropertiesFactory.Create(FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, Colors.Green)).ForegroundBrush as SolidColorBrush;
            Assert.That(foegroundBrush.Color, Is.EqualTo(Colors.Green));
        }

        [Test]
        public void Should_Set_The_Background()
        {
            var autoMoqer = new AutoMoqer();
            var textFormattingRunPropertiesFactory = autoMoqer.Create<TextFormattingRunPropertiesFactory>();
            var foegroundBrush = textFormattingRunPropertiesFactory.Create(FontAndColorsInfoFactory.CreateFontAndColorsInfo(false, default,Colors.Green)).BackgroundBrush as SolidColorBrush;
            Assert.That(foegroundBrush.Color, Is.EqualTo(Colors.Green));
        }

        
    }
}