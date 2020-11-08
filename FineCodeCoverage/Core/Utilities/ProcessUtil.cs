using System;
using CliWrap;
using System.IO;
using System.Linq;
using CliWrap.Buffered;
using System.Diagnostics;
using System.Threading.Tasks;

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

		public static async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request)
		{
			string shellScriptFile = null;
			string shellScriptOutputFile = null;

			try
			{
				// create script file

				shellScriptFile = Path.Combine(request.WorkingDirectory, $"{Path.GetFileNameWithoutExtension(request.FilePath)}-{Guid.NewGuid().ToString().Split('-').First()}.bat");
				shellScriptOutputFile = $"{shellScriptFile}.output";
				File.WriteAllText(shellScriptFile, $@"""{request.FilePath}"" {request.Arguments} > ""{shellScriptOutputFile}""");

				// run script file

				var result = await Cli
				.Wrap(shellScriptFile)
				.WithValidation(CommandResultValidation.None)
				.WithWorkingDirectory(request.WorkingDirectory)
				.ExecuteBufferedAsync();

				// get script output

				var output = File.Exists(shellScriptOutputFile)
					? File.ReadAllText(shellScriptOutputFile)
					: string.Join(Environment.NewLine, new[] { result.StandardOutput, result.StandardError }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim('\r', '\n').Trim();

				// return

				return new ExecuteResponse
				{
					ExitCode = result.ExitCode,
					ExitTime = result.ExitTime,
					RunTime = result.RunTime,
					StartTime = result.StartTime,
					Output = output
				};
			}
			finally
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(shellScriptFile) && File.Exists(shellScriptFile))
					{
						File.Delete(shellScriptFile);
					}
				}
				catch
				{
					// ignore
				}

				try
				{
					if (!string.IsNullOrWhiteSpace(shellScriptOutputFile) && File.Exists(shellScriptOutputFile))
					{
						File.Delete(shellScriptOutputFile);
					}
				}
				catch
				{
					// ignore
				}
			}
		}
	}

	internal class ExecuteRequest
	{
		public string FilePath { get; set; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
	}

	internal class ExecuteResponse
	{
		public int ExitCode { get; set; }
		public DateTimeOffset ExitTime { get; set; }
		public TimeSpan RunTime { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public string Output { get; set; }
	}
}
