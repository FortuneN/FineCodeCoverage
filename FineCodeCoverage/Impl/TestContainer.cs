using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class TestContainer : ReflectedType
    {
		public string Source { get; private set; }
		public TestContainer(object toReflect) : base(toReflect) { }
        protected override IEnumerable<PropertyReflection> GetPropertyReflections()
        {
			return new List<PropertyReflection>
			{
				new PropertyReflection { Name = nameof(Source),IsPublic = true, Setter = source => Source = (string)source}

			};
        }
    }
}
