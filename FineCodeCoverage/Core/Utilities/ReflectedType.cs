using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal abstract class ReflectedType
    {
		private bool isValid  = true;
        private readonly object typeToReflect;
		public object Actual => typeToReflect;
		 
        protected ReflectedType(object typeToReflect)
        {
            this.typeToReflect = typeToReflect;
        }
		protected ReflectedType(object ownerType, string propertyName, bool propertyIsPublic)
        {
			var reflectionValue = ReflectionHelper.InstancePropertyValue(ownerType, propertyName,propertyIsPublic);
            if (reflectionValue.Found)
            {
				this.typeToReflect = reflectionValue.Value;
            }
            else
            {
				isValid = false;
            }
		}
		protected abstract IEnumerable<PropertyReflection> GetPropertyReflections();
		public bool GetIsValid()
        {
            if (isValid)
            {
				foreach(var propertyReflection in GetPropertyReflections())
                {
					var reflectionValue = ReflectionHelper.InstancePropertyValue(typeToReflect, propertyReflection.Name, propertyReflection.IsPublic);
                    if (reflectionValue.Found)
                    {
						propertyReflection.Setter(reflectionValue.Value);
                    }
                    else
                    {
						return false;
                    }
				}
            }
            else
            {
				return false;
            }
			return true;
        }
		
	}
}
