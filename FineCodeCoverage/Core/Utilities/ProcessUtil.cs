using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IProcessUtil))]
    internal class ProcessUtil : IProcessUtil
    {
        public CancellationToken CancellationToken { get; set; }

        public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request)
        {
            var commandTask = Cli
            .Wrap(request.FilePath)
            .WithArguments(request.Arguments)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(request.WorkingDirectory)
            .ExecuteBufferedAsync(CancellationToken);

            BufferedCommandResult result = null; // result is null when cancelled
            ExecuteResponse executeResponse = null;
            try
            {
                result = await commandTask;
            }
            catch (OperationCanceledException)
            {
            }

            if (result != null)
            {
                var exitCode = result.ExitCode;

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
