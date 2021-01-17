using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
    public class Operation : ReflectObjectProperties
	{
		public Operation(object toReflect) : base(toReflect) { }
		public TestConfiguration Configuration { get; protected set; }

	}
}
