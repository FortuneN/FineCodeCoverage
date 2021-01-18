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
		private static MethodInfo createEnumerableMethodInfo;
		private const BindingFlags defaultBindingFlags = BindingFlags.Instance | BindingFlags.Public;
		public object ReflectedObject { get; }
		public Type ReflectedType { get; }
		private Type listType = typeof(List<>);
		private Type enumerableTTYpe = typeof(IEnumerable<>);


		static ReflectObjectProperties()
		{
			createEnumerableMethodInfo = typeof(ReflectObjectProperties).GetMethod(nameof(CreateEnumerable), BindingFlags.NonPublic | BindingFlags.Instance);

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
		private object ReflectedPropertyValue(string propertyName, BindingFlags bindingFlags)
		{
			object value = null;
			var reflectedProperty = ReflectedType.GetProperty(propertyName, bindingFlags);
			if (reflectedProperty != null)
			{
				value = reflectedProperty.GetValue(ReflectedObject);
			}
			return value;
		}
		private IEnumerable<T> CreateEnumerable<T>(IEnumerable value)
		{
			var enumerator = (value as IEnumerable).GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return WrapTyped<T>(enumerator.Current);
			}
		}
		private T WrapTyped<T>(object toReflect)
		{
			return (T)Wrap(typeof(T), toReflect);
		}
		private object Wrap(Type type, object toReflect)
		{
			return Activator.CreateInstance(type, toReflect);
		}

		private bool IsReflectObjectPropertiesType(Type type)
		{
			return typeof(ReflectObjectProperties).IsAssignableFrom(type);
		}

		private object CoerceValue(object value, Type ownPropertyType)
		{
			if (IsReflectObjectPropertiesType(ownPropertyType))
			{
				value = Wrap(ownPropertyType, value);
			}
			else
			{
				/*
					implement another time - List<> and IEnumerable<> will be sufficient
					if (ownPropertyType.IsArray)
					{
						var elementType = ownPropertyType.GetElementType();
						...

					}
				*/
				if (ownPropertyType.IsGenericType)
				{
					var genericArguments = ownPropertyType.GetGenericArguments();
					var genericArgument = genericArguments[0];
					if (genericArguments.Length == 1 && IsReflectObjectPropertiesType(genericArgument))
					{
						var genericTypeDefinitionType = ownPropertyType.GetGenericTypeDefinition();

						if (genericTypeDefinitionType == listType)
						{
							var listType = typeof(List<>).MakeGenericType(genericArgument);
							var addMethod = listType.GetMethod("Add");
							var list = Activator.CreateInstance(listType);
							var enumerator = (value as IEnumerable).GetEnumerator();
							while (enumerator.MoveNext())
							{
								addMethod.Invoke(list, new object[] { Wrap(genericArgument, enumerator.Current) });
							}
							value = list;

						}
						else if (genericTypeDefinitionType == enumerableTTYpe)
						{
							value = GetCreateEnumerableMethodInfo(genericArgument).Invoke(this, new object[] { value });
						}
					}


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
		private MethodInfo GetCreateEnumerableMethodInfo(Type type)
		{
			return createEnumerableMethodInfo.MakeGenericMethod(type);
		}

	}

}
