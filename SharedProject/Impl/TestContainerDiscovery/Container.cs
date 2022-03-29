using ReflectObject;
using System.Reflection;

namespace FineCodeCoverage.Impl
{
    public class Container : ReflectObjectProperties
    {
		public Container(object toReflect) : base(toReflect) { }
		public string ProjectName { get; protected set; }
		public string Source { get; protected set; }
		public object TargetPlatform { get; protected set; }

		// this is a public enum FrameworkVersion
		//[ReflectFlags(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic)]
		public object TargetFramework { get; protected set; }
		public ContainerData ProjectData { get; protected set; }
	}
}
