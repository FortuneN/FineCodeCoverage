using NUnit.Framework;
using Org.XmlUnit.Builder;

namespace FineCodeCoverageTests.TestHelpers
{
    internal static class XmlAssert
    {
        public static void NoXmlDifferences(string actual, string expected)
        {
            var diff = DiffBuilder.Compare(Input.FromString(expected)).WithTest(Input.FromString(actual)).Build();
            Assert.IsFalse(diff.HasDifferences());
        }
    }
}
