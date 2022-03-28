using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    public static class ReflectionExtensions
    {
        public static TCustomAttribute[] GetTypedCustomAttributes<TCustomAttribute>(this ICustomAttributeProvider customAttributeProvider, bool inherit) where TCustomAttribute:Attribute
        {
            var attributes = customAttributeProvider.GetCustomAttributes(typeof(TCustomAttribute), inherit);
            return attributes as TCustomAttribute[];
        }

        public static PropertyInfo[] GetInterfacePropertyInfos(this Type type)
        {
            return (new Type[] { type })
                   .Concat(type.GetInterfaces())
                   .SelectMany(i => i.GetProperties()).ToArray();
        }
    }
}
