using FineCodeCoverage.Core.Initialization;
using NUnit.Framework;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverageTests.Test_helpers
{
    internal static class ExportsInitializable
    {
        public static void Should_Export_IInitializable(Type type)
        {
            var exportsIInitializable = type.GetCustomAttributes(typeof(ExportAttribute), false).Any(ea => (ea as ExportAttribute).ContractType == typeof(IInitializable));
            Assert.That(exportsIInitializable, Is.True);
            Assert.That(type.GetInterfaces().Any(i => i == typeof(IInitializable)), Is.True);
        }
    }
}
