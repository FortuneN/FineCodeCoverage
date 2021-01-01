using System;

namespace FineCodeCoverage.Core.Utilities
{
	public class MicroserverException : Exception
	{
		public string Type { get; }

		public MicroserverException(string type, Exception innerException) : base(innerException.Message, innerException)
		{
			if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
			if (innerException == null) throw new ArgumentNullException(nameof(innerException));

			Type = type;
		}

		public MicroserverException(string type, string message) : base(message)
		{
			if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
			if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

			Type = type;
		}

		public MicroserverException(string type, string message, Exception innerException) : base(message, innerException)
		{
			if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
			if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
			if (innerException == null) throw new ArgumentNullException(nameof(innerException));

			Type = type;
		}
	}
}
