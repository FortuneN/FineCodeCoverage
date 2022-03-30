using System;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverage.Core.Utilities
{
    public static class ReflectionExtensions
    {
        public static TCustomAttribute[] GetTypedCustomAttributes<TCustomAttribute>(this ICustomAttributeProvider customAttributeProvider, bool inherit) where TCustomAttribute:Attribute
        {
            var attributes = customAttributeProvider.GetCustomAttributes(typeof(TCustomAttribute), inherit);
            return attributes as TCustomAttribute[];
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (!type.IsInterface)
                return type.GetProperties();

            return (new Type[] { type })
                   .Concat(type.GetInterfaces())
                   .SelectMany(i => i.GetProperties()).ToArray();
        }

    }
}
