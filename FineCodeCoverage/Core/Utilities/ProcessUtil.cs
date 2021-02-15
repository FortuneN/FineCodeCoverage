using System;
using CliWrap;
using System.IO;
using System.Linq;
using CliWrap.Buffered;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Engine.Utilities
{
	interface IProcessUtil
    {
		Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request);
		CancellationToken CancellationToken { get; set; }

	}

	[Export(typeof(IProcessUtil))]
	internal class ProcessUtil : IProcessUtil
	{
		public const int FAILED_TO_PRODUCE_OUTPUT_FILE_CODE = 999;
        private readonly ILogger logger;

        [ImportingConstructor]
		public ProcessUtil(ILogger logger)
        {
            this.logger = logger;
        }

		public CancellationToken CancellationToken { get; set; }
		
		public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request)
		{
			string shellScriptFile = null;
			string shellScriptOutputFile = null;

			// create script file

			shellScriptFile = Path.Combine(request.WorkingDirectory, $"{Guid.NewGuid().ToString().Split('-').First()}.bat");
			shellScriptOutputFile = $"{shellScriptFile}.output";
			File.WriteAllText(shellScriptFile, $@"""{request.FilePath}"" {request.Arguments} > ""{shellScriptOutputFile}""");

			// run script file

			var commandTask = Cli
			.Wrap(shellScriptFile)
			.WithValidation(CommandResultValidation.None)
			.WithWorkingDirectory(request.WorkingDirectory)
			.ExecuteBufferedAsync(CancellationToken);

			BufferedCommandResult result = null; // result is null when cancelled
			ExecuteResponse executeResponse = null;
			try
            {
				result = await commandTask;
			}
			catch(OperationCanceledException)
            {
            }

			if (result != null)
            {
				var exitCode = result.ExitCode;
				logger.Log("Exit code: " + exitCode);

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
					logger.Log("Switching exit code as failed to produce output file");
				}

				var output = string.Join(Environment.NewLine, outputList)
				.Trim('\r', '\n')
				.Trim();

				executeResponse = new ExecuteResponse
				{
					ExitCode = exitCode,
					ExitTime = result.ExitTime,
					RunTime = result.RunTime,
					StartTime = result.StartTime,
					Output = output
				};
			}

			FileSystemInfoDeleteExtensions.TryDelete(shellScriptFile);
			FileSystemInfoDeleteExtensions.TryDelete(shellScriptOutputFile);

			return executeResponse;
		}
		
	}

	internal static class ProcessExtensions
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
