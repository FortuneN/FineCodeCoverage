using System.Reflection;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Impl
{
    public class ContainerData : ReflectObjectProperties {
		public ContainerData(object toReflect) : base(toReflect) { }
		[ReflectFlags(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic)]
		public string ProjectFilePath { get; protected set; }
	}
}
