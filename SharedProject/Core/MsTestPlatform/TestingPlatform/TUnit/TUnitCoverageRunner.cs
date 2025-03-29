using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitCoverageRunner))]
    internal class TUnitCoverageRunner : ITUnitCoverageRunner
    {
        private const string zipDirectoryName = "dotnet-coverage";
        private const string zipPrefix = "dotnet-coverage";
        private readonly ILogger logger;
        private readonly IToolUnzipper toolUnzipper;
        private const int successExitCode = 0;
        private readonly Dictionary<int, string> nonSuccessExitCodeMessages = new Dictionary<int, string>
        {
            { 2, "At least one test failure." },
            { 3, "Test session was aborted." },
            { 4, "Setup of used extension is invalid."},
            { 5, "Command line arguments are invalid."},
            { 6, "Test session is using a non-implemented feature." },
            { 7, "Test session was unable to complete successfully, and likely crashed. It's possible that this was caused by a test session that was run via a test controller's extension point."},
            // todo check the source for this one as may be the minimum expected tests setting
            { 8, "Test session ran 0 tests." },
            { 9, "Minimum execution policy for the executed tests was violated." },
            { 10, "The test adapter failed to run tests for an infrastructure reason unrelated to the test's self.  An example is failing to create a fixture needed by tests." },
            { 11, "The test process will exit if dependent process exits" },
            { 12, "Test session was unable to run because the client does not support any of the supported protocol versions." },
            { 13, "Test session was stopped due to reaching the specified number of maximum failed tests using --maximum-failed-tests command-line option." }
        };

        public event EventHandler ReadyEvent;

        [ImportingConstructor]
        public TUnitCoverageRunner(
            ILogger logger,
            IToolUnzipper toolUnzipper
        )
        {
            this.logger = logger;
            this.toolUnzipper = toolUnzipper;
        }

        private (string,string) GetExeAndArgs(
            TUnitSettings tUnitSettings,
            bool hasCoverageExtension
        )
        {
            var path = hasCoverageExtension ? tUnitSettings.ExePath : dotnetCoverageExePath;
            var args = hasCoverageExtension ? $"--disable-logo --coverage --coverage-output-format cobertura --coverage-settings \"{tUnitSettings.SettingsPath}\" --coverage-output  \"{tUnitSettings.OutputPath}\"" :
                    $"collect \"{tUnitSettings.ExePath}\" --disable-logo -f cobertura -o \"{tUnitSettings.OutputPath}\" -s \"{tUnitSettings.SettingsPath}\" --nologo";
            args = $"{args} {tUnitSettings.AdditionalArgs}";
            return (path, args);
        }

        private CancellationToken cancellationToken;
        private string dotnetCoverageExePath;

        public async Task<bool> RunAsync(
            TUnitSettings tUnitSettings,
            bool hasCoverageExtension,
            bool showWindow = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.cancellationToken = cancellationToken;
            var (path,args) = GetExeAndArgs(tUnitSettings, hasCoverageExtension);
            // could have FCC option - hide-test-output or just allow them to supply their own
            logger.Log("Executing TUnit", path, "Arguments", args);
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = !showWindow,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                cancellationToken.ThrowIfCancellationRequested();
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);
                process.WaitForExit(1000); // Ensures all output is handled

                /*
                    from https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli#run-and-debug-tests
                	The app exits with a nonzero exit code if there's an error, which is typical for most executables. For more information on the known exit codes, see Microsoft.Testing.Platform exit codes.
					Tip
				    You can ignore a specific exit code using the --ignore-exit-code command line option.

                */
                LogNonSuccessExitCode(process.ExitCode);
                logger.Log("-----------");
                return process.ExitCode == successExitCode;
            }
        }

        private void LogNonSuccessExitCode(int exitCode)
        {
            if(exitCode != successExitCode)
            {
                string message = $"Non success exit code : {exitCode}.";
                if(nonSuccessExitCodeMessages.TryGetValue(exitCode, out var msg))
                {
                    message = $"{message}  {msg}";
                }
                logger.Log(message);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                logger.Log($"Error: {e.Data}");
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                logger.Log(e.Data);
            }
        }

        public void Initialize(string appDataFolderPath, CancellationToken cancellationToken)
        {
            var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolderPath, zipDirectoryName, zipPrefix, cancellationToken);
            dotnetCoverageExePath = Directory.GetFiles(zipDestination, "dotnet-coverage.exe", SearchOption.AllDirectories).First();
            ReadyEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
