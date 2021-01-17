using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
	public abstract class ReflectObjectProperties
	{
		private const BindingFlags defaultBindingFlags = BindingFlags.Instance | BindingFlags.Public;
		public object ReflectedObject { get; }
		public Type ReflectedType { get; }

		public Type IEnumerableOfTTypeArgument(Type type)
		{
			if (type == typeof(string))
			{
				return null;
			}

			if (type.IsInterface && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				return type.GetGenericArguments()[0];
			foreach (Type intType in type.GetInterfaces())
			{
				if (intType.IsGenericType
					&& intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					return intType.GetGenericArguments()[0];
				}
			}
			return null;
		}
		private BindingFlags GetBindingFlags(PropertyInfo ownProperty)
        {
			var bindingFlags = defaultBindingFlags;
			var reflectFlags = ownProperty.GetCustomAttribute<ReflectFlagsAttribute>();
			if (reflectFlags != null)
			{
				bindingFlags = reflectFlags.BindingFlags;
			}
			return bindingFlags;
		}
		private object ReflectedPropertyValue(string propertyName,BindingFlags bindingFlags)
        {
			object value = null;
			var reflectedProperty = ReflectedType.GetProperty(propertyName, bindingFlags);
			if(reflectedProperty != null)
            {
				value = reflectedProperty.GetValue(ReflectedObject);
			}
			return value;
		}
		private object CoerceValue(object value,Type ownPropertyType)
        {
			if ((typeof(ReflectObjectProperties).IsAssignableFrom(ownPropertyType)))
			{
				value = Activator.CreateInstance(ownPropertyType, value);
			}
			else
			{
				var enumerableTypeArgument = IEnumerableOfTTypeArgument(ownPropertyType);
				if (enumerableTypeArgument != null && typeof(ReflectObjectProperties).IsAssignableFrom(enumerableTypeArgument))
				{
					var listType = typeof(List<>).MakeGenericType(enumerableTypeArgument);
					var addMethod = listType.GetMethod("Add");
					var list = Activator.CreateInstance(listType);
					var enumerator = (value as IEnumerable).GetEnumerator();
					while (enumerator.MoveNext())
					{
						addMethod.Invoke(list, new object[] { Activator.CreateInstance(enumerableTypeArgument, enumerator.Current) });
					}
					value = list;
				}
			}
			return value;
		}
		private IEnumerable<PropertyInfo> GetOwnSettableProperties()
        {
			var excludeProperties = new List<string> { nameof(ReflectedObject), nameof(ReflectedType) };
			var ownProperties = this.GetType().GetProperties();
			return ownProperties.Where(p => !excludeProperties.Contains(p.Name));
		}
		public ReflectObjectProperties(object toReflect)
		{
			ReflectedObject = toReflect;
			ReflectedType = toReflect.GetType();
			foreach (var ownProperty in GetOwnSettableProperties())
			{
				var value = ReflectedPropertyValue(ownProperty.Name, GetBindingFlags(ownProperty));
				if (value != null)
				{
					value = CoerceValue(value, ownProperty.PropertyType);
					ownProperty.SetValue(this, value);
				}
				
			}
		}
	}
}
