using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using FineCodeCoverage.Editor.Tagging.Classification;
using FineCodeCoverage.Editor.Tagging.GlyphMargin;
using FineCodeCoverage.Editor.Tagging.OverviewMargin;
using Microsoft.VisualStudio.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.Tagging
{
    internal class TaggerProviders_LanguageSupport_Tests
    {
        [Test]
        public void Should_Only_Be_Interested_In_CSharp_VB_And_CPP()
        {
            var types = new List<Type> { typeof(CoverageLineGlyphFactoryProvider), typeof(CoverageLineGlyphTaggerProvider),typeof(CoverageLineClassificationTaggerProvider), typeof(CoverageLineOverviewMarkTaggerProvider)};
            types.ForEach(type =>
            {
                var contentTypeAttributes = type.GetCustomAttributes(typeof(ContentTypeAttribute), false);
                var contentTypes = contentTypeAttributes.OfType<ContentTypeAttribute>().Select(ct => ct.ContentTypes);
                Assert.That(contentTypes, Is.EqualTo(new[] { CSharpCoverageContentType.ContentType, VBCoverageContentType.ContentType, CPPCoverageContentType.ContentType, BlazorCoverageContentType.ContentType }));
            });
        }
    }
}
