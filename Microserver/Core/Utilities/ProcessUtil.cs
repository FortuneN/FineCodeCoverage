using CliWrap;
using CliWrap.Buffered;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
	public static class ProcessUtil
	{
		public const int FAILED_TO_PRODUCE_OUTPUT_FILE_CODE = 999;

		private static List<Process> Processes { get; } = new List<Process>();

		public static void ClearProcesses()
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

		public static async Task<(int ExitCode, DateTimeOffset ExitTime, TimeSpan RunTime, DateTimeOffset StartTime, string Output)> ExecuteAsync(string FilePath, string Arguments, string WorkingDirectory)
		{
			Process process = null;
			string shellScriptFile = null;
			string shellScriptOutputFile = null;

			try
			{
				// create script file

				shellScriptFile = Path.Combine(WorkingDirectory, $"{Guid.NewGuid().ToString().Split('-').First()}.bat");
				shellScriptOutputFile = $"{shellScriptFile}.output";
				File.WriteAllText(shellScriptFile, $@"""{FilePath}"" {Arguments} > ""{shellScriptOutputFile}""");

				// run script file

				var commandTask = Cli
				.Wrap(shellScriptFile)
				.WithValidation(CommandResultValidation.None)
				.WithWorkingDirectory(WorkingDirectory)
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

				return (
					ExitCode : exitCode,
					ExitTime : result.ExitTime,
					RunTime : result.RunTime,
					StartTime : result.StartTime,
					Output : output
				);
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
}
