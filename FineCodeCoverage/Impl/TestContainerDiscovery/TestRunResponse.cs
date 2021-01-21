using ReflectObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    public class TestRunResponse:ReflectObjectProperties
    {
        public TestRunResponse(object toReflect) : base(toReflect) { }

        // Think that this has changed from public to internal - to be sure
        [ReflectFlags(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)]
        public long FailedTests { get; protected set; }
        [ReflectFlags(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        public long PassedTests { get; protected set; }
        [ReflectFlags(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        public long SkippedTests { get; protected set; }
        [ReflectFlags(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        public long TotalTests { get; protected set; }
        [ReflectFlags(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        public bool IsAborted { get; protected set; }

    }
}
