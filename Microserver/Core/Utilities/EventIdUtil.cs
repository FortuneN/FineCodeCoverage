using Microsoft.Extensions.Logging;

namespace FineCodeCoverage.Core.Utilities
{
	public static class EventIdUtil
	{
		public static EventId New(string name)
		{
			return new EventId(name.GetHashCode(), name);
		}
	}
}
