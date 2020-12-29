using System.Collections.Generic;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Impl
{
    internal class TestConfiguration:ReflectedType, ITestConfiguration
	{
		public string ResultsDirectory { get; private set; }
		public object UserRunSettings { get; private set; }
		public IEnumerable<object> Containers { get; private set; }
		public TestConfiguration(IOperation operation):base(operation,"Configuration",true)
        {
			
		}

        protected override IEnumerable<PropertyReflection> GetPropertyReflections()
        {
			var propertyReflections = new List<PropertyReflection>
			{
				new PropertyReflection { Name = nameof(ResultsDirectory),IsPublic = true, Setter = value => ResultsDirectory = value as string
				},
				new PropertyReflection { Name = nameof(UserRunSettings),IsPublic = true, Setter = value => UserRunSettings = value
				},
				new PropertyReflection { Name = nameof(Containers),IsPublic = true, Setter = value => Containers = value as IEnumerable<object>
				},
			};
			return propertyReflections;
        }
    }
}
