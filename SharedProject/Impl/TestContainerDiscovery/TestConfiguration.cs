using System.Collections.Generic;
using ReflectObject;

namespace FineCodeCoverage.Impl
{
    public class TestConfiguration : ReflectObjectProperties
	{
		public TestConfiguration(object toReflect) : base(toReflect) { }
		public object UserRunSettings { get; protected set; }
		public IEnumerable<Container> Containers { get; protected set; }
		public string SolutionDirectory { get; protected set; }
	}
}
