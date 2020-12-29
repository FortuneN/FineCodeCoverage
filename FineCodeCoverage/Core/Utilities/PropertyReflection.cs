using System;

namespace FineCodeCoverage.Impl
{
    internal class PropertyReflection
	{
		public string Name { get; set; }
		public bool IsPublic { get; set; }
		public Action<object> Setter { get; set; }
	}
}
