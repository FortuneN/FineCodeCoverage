using System;
using System.Diagnostics;
using System.Linq;

namespace FineCodeCoverage.Engine.Utilities
{
	internal static class ProcessUtil
	{
		public static string GetOutput(this Process process)
		{
			return string.Join(
				Environment.NewLine,
				new[]
				{
					process.StandardOutput?.ReadToEnd(),
					process.StandardError?.ReadToEnd()
				}
				.Where(x => !string.IsNullOrWhiteSpace(x))
			);
		}
	}
}
