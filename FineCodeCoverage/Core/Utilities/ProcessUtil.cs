using System;
using CliWrap;
using System.IO;
using System.Linq;
using CliWrap.Buffered;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Utilities
{
	internal static class ProcessUtil
	{
		public const int FAILED_TO_PRODUCE_OUTPUT_FILE_CODE = 999;

		private static List<Process> Processes { get; } = new List<Process>();

		public static void ClearProcesses()
		{
			try
			{
				Processes.ToArray().AsParallel().ForAll(process =>
				{
					try
					{
						process.Kill();
					}
					catch
					{
						// ignore
					}
					finally
					{
						Processes.Remove(process);
					}
				});
			}
			catch
			{
				// ignore
			}
		}

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
			Process process = null;
			string shellScriptFile = null;
			string shellScriptOutputFile = null;

			try
			{
				// create script file

				shellScriptFile = Path.Combine(request.WorkingDirectory, $"{Guid.NewGuid().ToString().Split('-').First()}.bat");
				shellScriptOutputFile = $"{shellScriptFile}.output";
				File.WriteAllText(shellScriptFile, $@"""{request.FilePath}"" {request.Arguments} > ""{shellScriptOutputFile}""");

				// run script file

				var commandTask = Cli
				.Wrap(shellScriptFile)
				.WithValidation(CommandResultValidation.None)
				.WithWorkingDirectory(request.WorkingDirectory)
				.ExecuteBufferedAsync();

				// enlist process

				try
				{
					process = Process.GetProcessById(commandTask.ProcessId);
					if (process != null) Processes.Add(process);
				}
				catch
				{
					// ignore
				}

				// run command

				var result = await commandTask;
				var exitCode = result.ExitCode;

				// get script output

				var outputList = new List<string>();

				var directOutput = string.Join(Environment.NewLine, new[]
				{
					result.StandardOutput,
					Environment.NewLine,
					result.StandardError
				}
				.Where(x => !string.IsNullOrWhiteSpace(x)))
				.Trim('\r', '\n')
				.Trim();

				if (!string.IsNullOrWhiteSpace(directOutput))
				{
					outputList.Add(directOutput);
				}

				if (File.Exists(shellScriptOutputFile))
				{
					var redirectOutput = File.ReadAllText(shellScriptOutputFile)
					.Trim('\r', '\n')
					.Trim();

					if (!string.IsNullOrWhiteSpace(redirectOutput))
					{
						outputList.Add(redirectOutput);
					}
				}
				else
				{
					// There is a problem if the shellScriptOutputFile is not produced
					exitCode = FAILED_TO_PRODUCE_OUTPUT_FILE_CODE;
				}

				var output = string.Join(Environment.NewLine, outputList)
				.Trim('\r', '\n')
				.Trim();

				// return

				return new ExecuteResponse
				{
					ExitCode = exitCode,
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
					File.Delete(shellScriptFile);
				}
				catch
				{
					// ignore
				}

				try
				{
					File.Delete(shellScriptOutputFile);
				}
				catch
				{
					// ignore
				}

				try
				{
					process?.Kill();
				}
				catch
				{
					// ignore
				}

				try
				{
					process?.Dispose();
				}
				catch
				{
					// ignore
				}

				try
				{
					Processes.Remove(process);
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
