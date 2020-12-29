using System.Reflection;

namespace FineCodeCoverage.Impl
{
    internal static class ReflectionHelper
    {
		
		public static ReflectionValue PublicInstancePropertyValue(object owner, string propertyName)
        {
			return InstancePropertyValue (owner, propertyName, true);

		}

		public static ReflectionValue NonPublicInstancePropertyValue(object owner, string propertyName)
		{
			return InstancePropertyValue (owner, propertyName, false);

		}

		public static ReflectionValue InstancePropertyValue(object owner, string propertyName, bool isPublic)
		{
			var propertyInfo = owner.GetType().GetProperty(propertyName,( isPublic? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance);
			if (propertyInfo != null)
			{
				if (propertyInfo != null)
				{
					return new ReflectionValue { Value = propertyInfo.GetValue(owner)};
				}
			}
			return new ReflectionValue { Found = false };
		}

	}
}
