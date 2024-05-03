﻿using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class BlazorCoverageContentType_Tests
    {
        [TestCase("path.razor",false)]
        [TestCase("path.cshtml", true)]
        [TestCase("path.vbhtml", true)]
        public void Should_Include_Razor_Component_Files(string filePath, bool expectedExclude)
        {
            Assert.That(new BlazorCoverageContentType(null).Exclude(filePath), Is.EqualTo(expectedExclude));
        }
    }
}