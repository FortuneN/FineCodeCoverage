using FineCodeCoverage.Core.Utilities;
using System.Reflection;

namespace FineCodeCoverage.Impl
{
    public class Operation : ReflectObjectProperties
	{
		public Operation(object toReflect) : base(toReflect) { }
		[ReflectFlags(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
		public TestConfiguration Configuration { get; protected set; }

	}
}
