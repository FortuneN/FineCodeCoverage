using System;
using System.Reflection;

namespace FineCodeCoverage.Core.Utilities
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ReflectFlagsAttribute : Attribute
	{
		public ReflectFlagsAttribute(BindingFlags bindingFlags)
		{
			BindingFlags = bindingFlags;
		}

		public BindingFlags BindingFlags { get; }
	}

}
