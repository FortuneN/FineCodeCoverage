using ReflectObject;

namespace FineCodeCoverage.Impl
{
    public class Container : ReflectObjectProperties
    {
        public Container(object toReflect) : base(toReflect) { }
        public string ProjectName { get; protected set; }
        public string Source { get; protected set; }
        public object TargetPlatform { get; protected set; }
        public ContainerData ProjectData { get; protected set; }
    }
}
